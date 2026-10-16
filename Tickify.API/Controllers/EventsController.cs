using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickify.API.Extensions;
using Tickify.Core.DTOs.Common;
using Tickify.Core.DTOs.Events;
using Tickify.Core.DTOs.Seats;
using Tickify.Core.Enums;
using Tickify.Core.Interfaces.Services;

namespace Tickify.API.Controllers;

[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class EventsController : ApiControllerBase
{
    private readonly IEventService _events;

    public EventsController(IEventService events) => _events = events;

    /// <summary>Lists events with filtering, search, sorting and paging. Users see upcoming events only.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EventResponse>>> GetAll([FromQuery] EventQueryParameters query, CancellationToken ct) =>
        ToActionResult(await _events.GetPagedAsync(query, User.IsAdmin(), ct));

    /// <summary>Gets an event with its seat map dimensions and available seat count.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailsResponse>> GetById([FromRoute] int id, CancellationToken ct) =>
        ToActionResult(await _events.GetByIdAsync(id, ct));

    /// <summary>Gets the seat map of an event (available / sold).</summary>
    [HttpGet("{id:int}/seats")]
    [ProducesResponseType(typeof(List<SeatResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<SeatResponse>>> GetSeats([FromRoute] int id, CancellationToken ct) =>
        ToActionResult(await _events.GetSeatsAsync(id, ct));

    /// <summary>Creates an event together with all its seats (Admin).</summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(EventDetailsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDetailsResponse>> Create([FromBody] CreateEventRequest request, CancellationToken ct) =>
        ToActionResult(await _events.CreateAsync(request, ct),
            value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));

    /// <summary>Updates event details and categories, not the seat structure (Admin).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateEventRequest request, CancellationToken ct) =>
        ToActionResult(await _events.UpdateAsync(id, request, ct));

    /// <summary>Deletes an event that has no sold tickets (Admin).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct) =>
        ToActionResult(await _events.DeleteAsync(id, ct));
}
