using AssetTagBD.Application.Features.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetTagBD.API.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin,ITManager")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken) =>
        ToActionResult(await _mediator.Send(new GetDashboardSummaryQuery(), cancellationToken));
}
