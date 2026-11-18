using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Assets;
using MediatR;

namespace AssetTagBD.Application.Features.Assets;

/// <summary>Assets currently checked out to the signed-in user.</summary>
public sealed record GetMyAssetsQuery : IRequest<Result<IReadOnlyList<AssetDto>>>;

public sealed class GetMyAssetsQueryHandler : IRequestHandler<GetMyAssetsQuery, Result<IReadOnlyList<AssetDto>>>
{
    private readonly IAssetRepository _assets;
    private readonly ICurrentUserService _currentUser;

    public GetMyAssetsQueryHandler(IAssetRepository assets, ICurrentUserService currentUser)
    {
        _assets = assets;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<AssetDto>>> Handle(
        GetMyAssetsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Result.Failure<IReadOnlyList<AssetDto>>(
                AppErrors.Unauthorized("Authentication is required."));
        }

        var assets = await _assets.GetAssignedToUserAsync(_currentUser.UserId.Value, cancellationToken);

        var dtos = assets.Select(asset => AssetDto.FromEntity(asset)).ToList();
        return Result.Success<IReadOnlyList<AssetDto>>(dtos);
    }
}
