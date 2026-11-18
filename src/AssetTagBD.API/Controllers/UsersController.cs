using AssetTagBD.Application.Features.Users;
using AssetTagBD.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetTagBD.API.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin,ITManager")]
public sealed class UsersController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] UserRole? role, CancellationToken cancellationToken) =>
        ToActionResult(await _mediator.Send(new GetUsersQuery(role), cancellationToken));
}
