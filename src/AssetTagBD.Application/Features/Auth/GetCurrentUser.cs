using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Users;
using MediatR;

namespace AssetTagBD.Application.Features.Auth;

/// <summary>Lets the SPA restore the signed-in user from a stored JWT after a reload.</summary>
public sealed record GetCurrentUserQuery : IRequest<Result<UserDto>>;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _users;

    public GetCurrentUserQueryHandler(ICurrentUserService currentUser, IUserRepository users)
    {
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Result.Failure<UserDto>(AppErrors.Unauthorized("Authentication is required."));
        }

        var user = await _users.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);

        return user is null
            ? Result.Failure<UserDto>(AppErrors.Unauthorized("The current user no longer exists."))
            : Result.Success(UserDto.FromEntity(user));
    }
}
