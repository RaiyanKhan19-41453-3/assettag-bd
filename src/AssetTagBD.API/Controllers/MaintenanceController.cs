using AssetTagBD.Application.Features.Maintenance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetTagBD.API.Controllers;

[ApiController]
[Authorize]
public sealed class MaintenanceController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public MaintenanceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("report")]
    public async Task<IActionResult> Report([FromBody] CreateMaintenanceLogCommand command) =>
        ToActionResult(await _mediator.Send(command));

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Roles = "SuperAdmin,ITManager")]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _mediator.Send(new ResolveMaintenanceLogCommand(id), cancellationToken));

    [HttpGet("asset/{assetId:guid}")]
    public async Task<IActionResult> GetByAsset(Guid assetId, CancellationToken cancellationToken) =>
        ToActionResult(await _mediator.Send(new GetMaintenanceLogsQuery(assetId), cancellationToken));
}
