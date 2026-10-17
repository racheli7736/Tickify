using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickify.API.Extensions;
using Tickify.Core.DTOs.Users;
using Tickify.Core.Interfaces.Services;

namespace Tickify.API.Controllers;

[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    /// <summary>Gets a user profile (name and role, no e-mail): the caller's own, or any user's for an Admin.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById([FromRoute] int id, CancellationToken ct) =>
        ToActionResult(await _users.GetByIdAsync(User.GetUserId(), User.IsAdmin(), id, ct));
}
