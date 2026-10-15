using Tickify.Core.Common;
using Tickify.Core.DTOs.Common;
using Tickify.Core.DTOs.Events;
using Tickify.Core.DTOs.Seats;

namespace Tickify.Core.Interfaces.Services;

public interface IEventService
{
    Task<ServiceResult<PagedResult<EventResponse>>> GetPagedAsync(EventQueryParameters query, bool isAdmin, CancellationToken ct);
    Task<ServiceResult<EventDetailsResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<ServiceResult<List<SeatResponse>>> GetSeatsAsync(int eventId, CancellationToken ct);
    Task<ServiceResult<EventDetailsResponse>> CreateAsync(CreateEventRequest request, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(int id, UpdateEventRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct);
}
