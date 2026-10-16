using Microsoft.Extensions.Logging;
using Moq;
using Tickify.Core.Common;
using Tickify.Core.DTOs.Common;
using Tickify.Core.DTOs.Orders;
using Tickify.Core.Entities;
using Tickify.Core.Enums;
using Tickify.Core.Exceptions;
using Tickify.Core.Interfaces.Repositories;
using Tickify.Service.Services;
using Tickify.Tests.Helpers;

namespace Tickify.Tests.Services;

public class OrderServiceTests
{
    private const int UserId = 7;

    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<ISeatRepository> _seats = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ListLogger<OrderService> _logger = new();

    private OrderService Sut() => new(_orders.Object, _events.Object, _seats.Object, _unitOfWork.Object, _logger);

    private static CreateOrderRequest Request(int eventId, params int[] seatIds) =>
        new() { EventId = eventId, SeatIds = seatIds.ToList() };

    private Order? _addedOrder;

    public OrderServiceTests()
    {
        // Remember the order the service asks the repository to add.
        _orders.Setup(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((o, _) => _addedOrder = o)
            .Returns(Task.CompletedTask);
    }

    private void GivenEventWithSeats(Event ev, List<Seat> seats)
    {
        _events.Setup(e => e.GetByIdWithDetailsAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _seats.Setup(s => s.GetByIdsForUpdateAsync(ev.Id, It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(seats);
    }

    private void VerifyNothingSaved() =>
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    // ---------- PurchaseAsync ----------

    [Fact]
    public async Task PurchaseAsync_AllSeatsAvailable_CreatesOrderAndMarksSeatsSold()
    {
        var ev = TestData.Event(price: 40m);
        var seats = TestData.AvailableSeats(2);
        GivenEventWithSeats(ev, seats);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var result = await Sut().PurchaseAsync(UserId, Request(ev.Id, 1, 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(80m, result.Value!.TotalPrice);
        Assert.Equal("R1-S1, R1-S2", result.Value.SeatsSummary);
        Assert.Equal(ev.Name, result.Value.EventName);
        Assert.NotNull(_addedOrder);
        Assert.Equal(UserId, _addedOrder!.UserId);
        Assert.Equal(OrderStatus.Active, _addedOrder.Status);
        Assert.All(seats, s => { Assert.Equal(SeatStatus.Sold, s.Status); Assert.Same(_addedOrder, s.Order); });
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Information);
    }

    [Fact]
    public async Task PurchaseAsync_SeatAlreadySold_ReturnsConflictAndSavesNothing()
    {
        var ev = TestData.Event();
        var seats = TestData.AvailableSeats(2);
        seats[1].Status = SeatStatus.Sold;
        GivenEventWithSeats(ev, seats);

        var result = await Sut().PurchaseAsync(UserId, Request(ev.Id, 1, 2), CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(OrderService.SeatConflictMessage, result.Error);
        Assert.Equal(SeatStatus.Available, seats[0].Status);
        _orders.Verify(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNothingSaved();
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task PurchaseAsync_ConcurrencyConflictOnSave_ReturnsConflictAndLogsWarning()
    {
        var ev = TestData.Event();
        GivenEventWithSeats(ev, TestData.AvailableSeats(1));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new ConcurrencyConflictException());

        var result = await Sut().PurchaseAsync(UserId, Request(ev.Id, 1), CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(OrderService.SeatConflictMessage, result.Error);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(_logger.Messages(LogLevel.Warning));
        Assert.Empty(_logger.Messages(LogLevel.Information));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public async Task PurchaseAsync_SeatCountOutsideOneToSix_ReturnsValidationErrorWithoutTouchingTheDatabase(int count)
    {
        var result = await Sut().PurchaseAsync(UserId, Request(1, Enumerable.Range(1, count).ToArray()), CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _events.VerifyNoOtherCalls();
        _seats.VerifyNoOtherCalls();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task PurchaseAsync_SixSeats_IsAllowed()
    {
        var ev = TestData.Event();
        GivenEventWithSeats(ev, TestData.AvailableSeats(6));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(7);

        var result = await Sut().PurchaseAsync(UserId, Request(ev.Id, 1, 2, 3, 4, 5, 6), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task PurchaseAsync_DuplicateSeatIds_ReturnsValidationError()
    {
        var result = await Sut().PurchaseAsync(UserId, Request(1, 3, 3), CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task PurchaseAsync_PastEvent_ReturnsValidationErrorAndDoesNotLoadSeats()
    {
        var ev = TestData.Event(startsAt: TestData.Past);
        _events.Setup(e => e.GetByIdWithDetailsAsync(ev.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await Sut().PurchaseAsync(UserId, Request(ev.Id, 1), CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _seats.VerifyNoOtherCalls();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task PurchaseAsync_SeatFromAnotherEvent_ReturnsValidationError()
    {
        var ev = TestData.Event();
        // Two seats were requested but only one belongs to the event (the repository filters by event).
        GivenEventWithSeats(ev, TestData.AvailableSeats(1));

        var result = await Sut().PurchaseAsync(UserId, Request(ev.Id, 1, 999), CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task PurchaseAsync_EventNotFound_ReturnsNotFound()
    {
        _events.Setup(e => e.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

        var result = await Sut().PurchaseAsync(UserId, Request(99, 1), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _seats.VerifyNoOtherCalls();
        VerifyNothingSaved();
    }

    // ---------- GetPagedAsync / GetByIdAsync ----------

    [Theory]
    [InlineData(false, UserId)]
    [InlineData(true, null)]
    public async Task GetPagedAsync_UserSeesOwnOrders_AdminSeesAll(bool isAdmin, int? expectedFilter)
    {
        var ev = TestData.Event();
        var paged = new PagedResult<Order>
        {
            Items = new List<Order> { TestData.ActiveOrder(1, UserId, ev, TestData.Seat(1)) },
            Page = 1, PageSize = 10, TotalCount = 1
        };
        _orders.Setup(o => o.GetPagedAsync(expectedFilter, It.IsAny<OrderQueryParameters>(), It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var result = await Sut().GetPagedAsync(UserId, isAdmin, new OrderQueryParameters(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal("Show", item.EventName);
        _orders.Verify(o => o.GetPagedAsync(expectedFilter, It.IsAny<OrderQueryParameters>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(UserId, false, ErrorType.None)]   // owner
    [InlineData(99, true, ErrorType.None)]        // admin
    [InlineData(99, false, ErrorType.Forbidden)]  // someone else
    public async Task GetByIdAsync_EnforcesOwnership(int callerId, bool isAdmin, ErrorType expected)
    {
        var order = TestData.ActiveOrder(5, UserId, TestData.Event(), TestData.Seat(1));
        _orders.Setup(o => o.GetByIdWithSeatsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await Sut().GetByIdAsync(callerId, isAdmin, 5, CancellationToken.None);

        Assert.Equal(expected, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownOrder_ReturnsNotFound()
    {
        _orders.Setup(o => o.GetByIdWithSeatsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var result = await Sut().GetByIdAsync(UserId, false, 5, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    // ---------- CancelAsync ----------

    [Fact]
    public async Task CancelAsync_ByOwnerBeforeEvent_ReleasesSeats()
    {
        var seats = new[] { TestData.Seat(1), TestData.Seat(2, number: 2) };
        var order = TestData.ActiveOrder(5, UserId, TestData.Event(), seats);
        _orders.Setup(o => o.GetByIdWithSeatsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var result = await Sut().CancelAsync(UserId, 5, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.All(seats, s => { Assert.Equal(SeatStatus.Available, s.Status); Assert.Null(s.OrderId); });
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_NotOwner_ReturnsForbiddenAndChangesNothing()
    {
        var seat = TestData.Seat(1);
        var order = TestData.ActiveOrder(5, UserId, TestData.Event(), seat);
        _orders.Setup(o => o.GetByIdWithSeatsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await Sut().CancelAsync(userId: 99, 5, CancellationToken.None);

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(OrderStatus.Active, order.Status);
        Assert.Equal(SeatStatus.Sold, seat.Status);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task CancelAsync_AfterEventDate_ReturnsValidationError()
    {
        var order = TestData.ActiveOrder(5, UserId, TestData.Event(startsAt: TestData.Past), TestData.Seat(1));
        _orders.Setup(o => o.GetByIdWithSeatsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await Sut().CancelAsync(UserId, 5, CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(OrderStatus.Active, order.Status);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelled_ReturnsValidationError()
    {
        var order = TestData.ActiveOrder(5, UserId, TestData.Event(), TestData.Seat(1));
        order.Status = OrderStatus.Cancelled;
        _orders.Setup(o => o.GetByIdWithSeatsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await Sut().CancelAsync(UserId, 5, CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task CancelAsync_UnknownOrder_ReturnsNotFound()
    {
        _orders.Setup(o => o.GetByIdWithSeatsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var result = await Sut().CancelAsync(UserId, 5, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CancelAsync_ConcurrencyConflictOnSave_ReturnsConflictAndLogsWarning()
    {
        var order = TestData.ActiveOrder(5, UserId, TestData.Event(), TestData.Seat(1));
        _orders.Setup(o => o.GetByIdWithSeatsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new ConcurrencyConflictException());

        var result = await Sut().CancelAsync(UserId, 5, CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Single(_logger.Messages(LogLevel.Warning));
    }
}
