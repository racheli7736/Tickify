using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickify.API.Extensions;
using Tickify.Core.DTOs.Common;
using Tickify.Core.DTOs.Orders;
using Tickify.Core.Enums;
using Tickify.Core.Interfaces.Services;

namespace Tickify.API.Controllers;

[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class OrdersController : ApiControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    /// <summary>Buys 1-6 seats for an event, all or nothing (User).</summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.User))]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Create([FromBody] CreateOrderRequest request, CancellationToken ct) =>
        ToActionResult(await _orders.PurchaseAsync(User.GetUserId(), request, ct),
            value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));

    /// <summary>Lists the caller's orders (Admin: all orders).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<OrderResponse>>> GetAll([FromQuery] OrderQueryParameters query, CancellationToken ct) =>
        ToActionResult(await _orders.GetPagedAsync(User.GetUserId(), User.IsAdmin(), query, ct));

    /// <summary>Gets one order (its owner, or an Admin).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById([FromRoute] int id, CancellationToken ct) =>
        ToActionResult(await _orders.GetByIdAsync(User.GetUserId(), User.IsAdmin(), id, ct));

    /// <summary>Cancels an active order of an event that has not started; the seats become available (owner only).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.User))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel([FromRoute] int id, CancellationToken ct) =>
        ToActionResult(await _orders.CancelAsync(User.GetUserId(), id, ct));
}
