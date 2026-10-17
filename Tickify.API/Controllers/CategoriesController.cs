using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickify.Core.DTOs.Categories;
using Tickify.Core.Enums;
using Tickify.Core.Interfaces.Services;

namespace Tickify.API.Controllers;

[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class CategoriesController : ApiControllerBase
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories) => _categories = categories;

    /// <summary>Lists all categories.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryResponse>>> GetAll(CancellationToken ct) =>
        ToActionResult(await _categories.GetAllAsync(ct));

    /// <summary>Gets one category.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetById([FromRoute] int id, CancellationToken ct) =>
        ToActionResult(await _categories.GetByIdAsync(id, ct));

    /// <summary>Creates a category (Admin).</summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create([FromBody] CategoryRequest request, CancellationToken ct) =>
        ToActionResult(await _categories.CreateAsync(request, ct),
            value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));

    /// <summary>Renames a category (Admin).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] CategoryRequest request, CancellationToken ct) =>
        ToActionResult(await _categories.UpdateAsync(id, request, ct));

    /// <summary>Deletes a category that is not assigned to any event (Admin).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct) =>
        ToActionResult(await _categories.DeleteAsync(id, ct));
}
