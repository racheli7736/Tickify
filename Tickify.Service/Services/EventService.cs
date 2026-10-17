using AutoMapper;
using Microsoft.Extensions.Logging;
using Tickify.Core.Common;
using Tickify.Core.DTOs.Common;
using Tickify.Core.DTOs.Events;
using Tickify.Core.DTOs.Seats;
using Tickify.Core.Entities;
using Tickify.Core.Enums;
using Tickify.Core.Interfaces.Repositories;
using Tickify.Core.Interfaces.Services;

namespace Tickify.Service.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _events;
    private readonly ISeatRepository _seats;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<EventService> _logger;

    public EventService(
        IEventRepository events,
        ISeatRepository seats,
        ICategoryRepository categories,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<EventService> logger)
    {
        _events = events;
        _seats = seats;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<PagedResult<EventResponse>>> GetPagedAsync(
        EventQueryParameters query, bool isAdmin, CancellationToken ct)
    {
        // Users only see upcoming events; admins see everything.
        var page = await _events.GetPagedAsync(query, includePast: isAdmin, ct);

        // One grouped query for the whole page (no N+1).
        var available = await _seats.GetAvailableCountsAsync(page.Items.Select(e => e.Id), ct);

        var items = _mapper.Map<List<EventResponse>>(page.Items);
        foreach (var item in items)
            item.AvailableSeats = available.GetValueOrDefault(item.Id);

        return ServiceResult<PagedResult<EventResponse>>.Success(new PagedResult<EventResponse>
        {
            Items = items,
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        });
    }

    public async Task<ServiceResult<EventDetailsResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var ev = await _events.GetByIdWithDetailsAsync(id, ct);
        if (ev is null)
            return ServiceResult<EventDetailsResponse>.Fail(ErrorType.NotFound, "Event not found.");

        var response = _mapper.Map<EventDetailsResponse>(ev);
        var available = await _seats.GetAvailableCountsAsync(new[] { id }, ct);
        var (rows, perRow) = await _seats.GetLayoutAsync(id, ct);
        response.AvailableSeats = available.GetValueOrDefault(id);
        response.Rows = rows;
        response.SeatsPerRow = perRow;

        return ServiceResult<EventDetailsResponse>.Success(response);
    }

    public async Task<ServiceResult<List<SeatResponse>>> GetSeatsAsync(int eventId, CancellationToken ct)
    {
        if (await _events.GetByIdWithDetailsAsync(eventId, ct) is null)
            return ServiceResult<List<SeatResponse>>.Fail(ErrorType.NotFound, "Event not found.");

        var seats = await _seats.GetByEventAsync(eventId, ct);
        return ServiceResult<List<SeatResponse>>.Success(_mapper.Map<List<SeatResponse>>(seats));
    }

    public async Task<ServiceResult<EventDetailsResponse>> CreateAsync(CreateEventRequest request, CancellationToken ct)
    {
        var startsAt = AsUtc(request.StartsAt);
        if (startsAt <= DateTime.UtcNow)
            return ServiceResult<EventDetailsResponse>.Fail(ErrorType.Validation, "The event date must be in the future.");

        var categoryIds = request.CategoryIds.Distinct().ToList();
        var categories = await _categories.GetByIdsAsync(categoryIds, ct);
        if (categories.Count != categoryIds.Count)
            return ServiceResult<EventDetailsResponse>.Fail(ErrorType.Validation, "One or more categories do not exist.");

        var ev = _mapper.Map<Event>(request);
        ev.StartsAt = startsAt;
        ev.CreatedAt = DateTime.UtcNow;
        // Link by id only: the categories came from a no-tracking query, and attaching them via
        // the navigation would make EF try to insert them again.
        foreach (var category in categories)
            ev.EventCategories.Add(new EventCategory { Event = ev, CategoryId = category.Id });

        var seats = new List<Seat>(request.Rows * request.SeatsPerRow);
        for (var row = 1; row <= request.Rows; row++)
            for (var number = 1; number <= request.SeatsPerRow; number++)
                seats.Add(new Seat { Event = ev, Row = row, Number = number, Status = SeatStatus.Available });

        // Event, its categories and all its seats are saved in a single call.
        await _events.AddAsync(ev, ct);
        await _seats.AddRangeAsync(seats, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Event {EventId} created with {SeatCount} seats", ev.Id, seats.Count);

        // Read the saved event back (reuses GetByIdAsync) so the response comes from the database.
        return await GetByIdAsync(ev.Id, ct);
    }

    public async Task<ServiceResult> UpdateAsync(int id, UpdateEventRequest request, CancellationToken ct)
    {
        var ev = await _events.GetByIdForUpdateAsync(id, ct);
        if (ev is null)
            return ServiceResult.Fail(ErrorType.NotFound, "Event not found.");

        // Only a *new* date has to be in the future; details of an event that already happened stay editable.
        var startsAt = AsUtc(request.StartsAt);
        if (startsAt != ev.StartsAt && startsAt <= DateTime.UtcNow)
            return ServiceResult.Fail(ErrorType.Validation, "The new event date must be in the future.");

        var categoryIds = request.CategoryIds.Distinct().ToList();
        var categories = await _categories.GetByIdsAsync(categoryIds, ct);
        if (categories.Count != categoryIds.Count)
            return ServiceResult.Fail(ErrorType.Validation, "One or more categories do not exist.");

        // Only details and categories are editable; the seat structure is fixed at creation.
        _mapper.Map(request, ev);
        ev.StartsAt = startsAt;

        foreach (var stale in ev.EventCategories.Where(ec => !categoryIds.Contains(ec.CategoryId)).ToList())
            ev.EventCategories.Remove(stale);
        foreach (var categoryId in categoryIds.Where(c => ev.EventCategories.All(ec => ec.CategoryId != c)))
            ev.EventCategories.Add(new EventCategory { EventId = ev.Id, CategoryId = categoryId });

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Event {EventId} updated", id);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken ct)
    {
        var ev = await _events.GetByIdForUpdateAsync(id, ct);
        if (ev is null)
            return ServiceResult.Fail(ErrorType.NotFound, "Event not found.");

        if (await _events.HasSoldSeatsAsync(id, ct) || await _events.HasOrdersAsync(id, ct))
            return ServiceResult.Fail(ErrorType.Conflict, "Cannot delete an event that has sold tickets.");

        _events.Remove(ev);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Event {EventId} deleted", id);
        return ServiceResult.Success();
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
