using Tickify.Core.DTOs.Common;
using Tickify.Core.DTOs.Events;
using Tickify.Core.Entities;

namespace Tickify.Core.Interfaces.Repositories;

public interface IEventRepository
{
    Task<PagedResult<Event>> GetPagedAsync(EventQueryParameters query, bool includePast, CancellationToken ct);
    Task<Event?> GetByIdWithDetailsAsync(int id, CancellationToken ct);
    Task<Event?> GetByIdForUpdateAsync(int id, CancellationToken ct);
    Task<bool> HasSoldSeatsAsync(int id, CancellationToken ct);
    Task AddAsync(Event ev, CancellationToken ct);
    void Remove(Event ev);
}
