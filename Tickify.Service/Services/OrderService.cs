using AutoMapper;
using Microsoft.Extensions.Logging;
using Tickify.Core.Common;
using Tickify.Core.DTOs.Common;
using Tickify.Core.DTOs.Orders;
using Tickify.Core.Entities;
using Tickify.Core.Enums;
using Tickify.Core.Exceptions;
using Tickify.Core.Interfaces.Repositories;
using Tickify.Core.Interfaces.Services;

namespace Tickify.Service.Services;

public class OrderService : IOrderService
{
    public const int MaxSeatsPerOrder = 6;

    public const string SeatConflictMessage =
        "אחד או יותר מהמושבים שבחרת נתפסו על ידי משתמש אחר. אנא בחרי מושבים מחדש.";

    private readonly IOrderRepository _orders;
    private readonly IEventRepository _events;
    private readonly ISeatRepository _seats;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orders,
        IEventRepository events,
        ISeatRepository seats,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<OrderService> logger)
    {
        _orders = orders;
        _events = events;
        _seats = seats;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<OrderResponse>> PurchaseAsync(int userId, CreateOrderRequest request, CancellationToken ct)
    {
        var seatIds = request.SeatIds ?? new List<int>();

        // Rules 1-2: 1..6 seats, no duplicates.
        if (seatIds.Count < 1 || seatIds.Count > MaxSeatsPerOrder)
            return Fail(ErrorType.Validation, $"An order must contain between 1 and {MaxSeatsPerOrder} seats.");
        if (seatIds.Distinct().Count() != seatIds.Count)
            return Fail(ErrorType.Validation, "The same seat cannot appear twice in an order.");

        // Rules 4-5: the event exists and has not started.
        var ev = await _events.GetByIdWithDetailsAsync(request.EventId, ct);
        if (ev is null)
            return Fail(ErrorType.NotFound, "Event not found.");
        if (ev.StartsAt <= DateTime.UtcNow)
            return Fail(ErrorType.Validation, "Tickets cannot be purchased for an event that has already started.");

        // Tracked on purpose: these rows are modified below, and EF uses their original RowVersion (xmin).
        var seats = await _seats.GetByIdsForUpdateAsync(ev.Id, seatIds, ct);

        // Rule 3: every requested seat exists and belongs to this event.
        if (seats.Count != seatIds.Count)
            return Fail(ErrorType.Validation, "One or more seats do not exist or do not belong to this event.");

        // Rule 6: every seat is still available right now.
        if (seats.Any(s => s.Status != SeatStatus.Available))
        {
            _logger.LogWarning(
                "Seat conflict (already sold) for user {UserId}, event {EventId}, seats {SeatIds}",
                userId, ev.Id, seatIds);
            return Fail(ErrorType.Conflict, SeatConflictMessage);
        }

        var ordered = seats.OrderBy(s => s.Row).ThenBy(s => s.Number).ToList();
        var order = new Order
        {
            UserId = userId,
            EventId = ev.Id,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Active,
            TotalPrice = ev.TicketPrice * ordered.Count,
            SeatsSummary = string.Join(", ", ordered.Select(s => $"R{s.Row}-S{s.Number}"))
        };

        foreach (var seat in ordered)
        {
            seat.Status = SeatStatus.Sold;
            seat.Order = order;
        }
        await _orders.AddAsync(order, ct);

        try
        {
            // The availability check and the write share this one SaveChanges: all or nothing.
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            // Rule 7: someone else bought one of the seats between our read and our write.
            _logger.LogWarning(
                "Seat conflict (concurrent purchase) for user {UserId}, event {EventId}, seats {SeatIds}",
                userId, ev.Id, seatIds);
            return Fail(ErrorType.Conflict, SeatConflictMessage);
        }

        _logger.LogInformation(
            "Order {OrderId} created by user {UserId} for event {EventId}, seats {SeatsSummary}",
            order.Id, userId, ev.Id, order.SeatsSummary);

        var response = _mapper.Map<OrderResponse>(order);
        response.EventName = ev.Name;
        response.EventStartsAt = ev.StartsAt;
        return ServiceResult<OrderResponse>.Success(response);
    }

    public async Task<ServiceResult<PagedResult<OrderResponse>>> GetPagedAsync(
        int userId, bool isAdmin, OrderQueryParameters query, CancellationToken ct)
    {
        var page = await _orders.GetPagedAsync(isAdmin ? null : userId, query, ct);

        return ServiceResult<PagedResult<OrderResponse>>.Success(new PagedResult<OrderResponse>
        {
            Items = _mapper.Map<List<OrderResponse>>(page.Items),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        });
    }

    public async Task<ServiceResult<OrderResponse>> GetByIdAsync(int userId, bool isAdmin, int orderId, CancellationToken ct)
    {
        var order = await _orders.GetByIdWithSeatsAsync(orderId, ct);
        if (order is null)
            return Fail(ErrorType.NotFound, "Order not found.");

        if (!isAdmin && order.UserId != userId)
            return Fail(ErrorType.Forbidden, "You do not have access to this order.");

        return ServiceResult<OrderResponse>.Success(_mapper.Map<OrderResponse>(order));
    }

    public async Task<ServiceResult> CancelAsync(int userId, int orderId, CancellationToken ct)
    {
        var order = await _orders.GetByIdWithSeatsAsync(orderId, ct);
        if (order is null)
            return ServiceResult.Fail(ErrorType.NotFound, "Order not found.");

        // Rule 8: only the owner may cancel.
        if (order.UserId != userId)
            return ServiceResult.Fail(ErrorType.Forbidden, "Only the owner of an order can cancel it.");

        // Rule 9: active order, event not yet started.
        if (order.Status != OrderStatus.Active)
            return ServiceResult.Fail(ErrorType.Validation, "Only an active order can be cancelled.");
        if (order.Event.StartsAt <= DateTime.UtcNow)
            return ServiceResult.Fail(ErrorType.Validation, "An order for an event that has already started cannot be cancelled.");

        foreach (var seat in order.Seats)
        {
            seat.Status = SeatStatus.Available;
            seat.OrderId = null;
        }
        order.Status = OrderStatus.Cancelled;

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Concurrency conflict while cancelling order {OrderId} by user {UserId}", orderId, userId);
            return ServiceResult.Fail(ErrorType.Conflict, "The order was modified by another request. Please try again.");
        }

        _logger.LogInformation("Order {OrderId} cancelled by user {UserId}", orderId, userId);
        return ServiceResult.Success();
    }

    private static ServiceResult<OrderResponse> Fail(ErrorType type, string message) =>
        ServiceResult<OrderResponse>.Fail(type, message);
}
