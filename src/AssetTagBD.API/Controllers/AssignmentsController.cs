using AssetTagBD.Application.Features.Assignments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetTagBD.API.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin,ITManager")]
public sealed class AssignmentsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AssignmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("assign")]
    public async Task<IActionResult> Assign([FromBody] AssignAssetCommand command) =>
        ToActionResult(await _mediator.Send(command));

    [HttpPost("return")]
    public async Task<IActionResult> Return([FromBody] ReturnAssetCommand command) =>
        ToActionResult(await _mediator.Send(command));

    [HttpGet("history/{assetId:guid}")]
    public async Task<IActionResult> History(Guid assetId, CancellationToken cancellationToken) =>
        ToActionResult(await _mediator.Send(new GetAssignmentHistoryQuery(assetId), cancellationToken));
}
