using Microsoft.AspNetCore.Mvc;
using Tickify.Core.Common;

namespace Tickify.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[ProducesErrorResponseType(typeof(ProblemDetails))] // every 4xx/5xx response documents the same ProblemDetails body
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>200 with the value, or the error mapped to its HTTP status.</summary>
    protected ActionResult<T> ToActionResult<T>(ServiceResult<T> result) =>
        ToActionResult(result, value => Ok(value));

    /// <summary>The given success response (e.g. 201), or the error mapped to its HTTP status.</summary>
    protected ActionResult<T> ToActionResult<T>(ServiceResult<T> result, Func<T, ActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value!) : Failure(result);

    /// <summary>204 No Content, or the error mapped to its HTTP status.</summary>
    protected IActionResult ToActionResult(ServiceResult result) =>
        result.IsSuccess ? NoContent() : Failure(result);

    private ObjectResult Failure(ServiceResult result)
    {
        var (status, title) = result.ErrorType switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Bad Request"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        return Problem(detail: result.Error, statusCode: status, title: title);
    }
}
