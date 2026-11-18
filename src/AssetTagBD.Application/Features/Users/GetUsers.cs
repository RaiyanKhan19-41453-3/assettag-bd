using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Users;
using AssetTagBD.Domain.Enums;
using MediatR;

namespace AssetTagBD.Application.Features.Users;

/// <summary>Provides the employee/manager picker used by the assignment UI.</summary>
public sealed record GetUsersQuery(UserRole? Role) : IRequest<Result<IReadOnlyList<UserDto>>>;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<IReadOnlyList<UserDto>>>
{
    private readonly IUserRepository _users;

    public GetUsersQueryHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await _users.GetAllAsync(cancellationToken);

        var dtos = users
            .Where(u => !request.Role.HasValue || u.Role == request.Role.Value)
            .OrderBy(u => u.FullName)
            .Select(UserDto.FromEntity)
            .ToList();

        return Result.Success<IReadOnlyList<UserDto>>(dtos);
    }
}
