using AssetTagBD.Application.Features.Audit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetTagBD.API.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin,ITManager")]
public sealed class AuditController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AuditController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        ToActionResult(await _mediator.Send(new GetAuditEntriesQuery(pageNumber, pageSize), cancellationToken));
}
