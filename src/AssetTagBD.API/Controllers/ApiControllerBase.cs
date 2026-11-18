using AssetTagBD.API.Models;
using AssetTagBD.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace AssetTagBD.API.Controllers;

/// <summary>
/// Maps the Application layer's Result failures onto consistent HTTP responses.
/// Controllers stay thin: validate input, dispatch a command/query, map the result.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ToActionResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : ToErrorResult(result.Error);

    protected IActionResult ToErrorResult(Error error)
    {
        var traceId = HttpContext.TraceIdentifier;
        var body = new ErrorResponse(error.Code, error.Message, traceId);

        return error.Code switch
        {
            "NotFound" => NotFound(body),
            "Conflict" => Conflict(body),
            "Unauthorized" => Unauthorized(body),
            "BadRequest" => BadRequest(body),
            _ => StatusCode(StatusCodes.Status500InternalServerError, body)
        };
    }
}
