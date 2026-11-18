using AssetTagBD.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetTagBD.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Public company sign-up: creates a tenant and its first SuperAdmin.</summary>
    [HttpPost("signup-company")]
    [AllowAnonymous]
    public async Task<IActionResult> SignupCompany([FromBody] CreateTenantCommand command) =>
        ToActionResult(await _mediator.Send(command));

    /// <summary>Invites a user into the caller's tenant. Tenant SuperAdmin only.</summary>
    [HttpPost("register")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command) =>
        ToActionResult(await _mediator.Send(command));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command) =>
        ToActionResult(await _mediator.Send(command));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command) =>
        ToActionResult(await _mediator.Send(command));

    [HttpPost("revoke")]
    [AllowAnonymous]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenCommand command) =>
        ToActionResult(await _mediator.Send(command));

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command) =>
        ToActionResult(await _mediator.Send(command));

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command) =>
        ToActionResult(await _mediator.Send(command));

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken) =>
        ToActionResult(await _mediator.Send(new GetCurrentUserQuery(), cancellationToken));
}
