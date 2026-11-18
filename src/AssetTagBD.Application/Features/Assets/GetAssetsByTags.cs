using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Assets;
using MediatR;

namespace AssetTagBD.Application.Features.Assets;

/// <summary>Batch lookup for printable QR label sheets (one request instead of N).</summary>
public sealed record GetAssetsByTagsQuery(IReadOnlyList<string> Tags)
    : IRequest<Result<IReadOnlyList<AssetDto>>>;

public sealed class GetAssetsByTagsQueryHandler
    : IRequestHandler<GetAssetsByTagsQuery, Result<IReadOnlyList<AssetDto>>>
{
    private const int MaxTags = 500;

    private readonly IAssetRepository _assets;
    private readonly IQrCodeService _qrCodeService;

    public GetAssetsByTagsQueryHandler(IAssetRepository assets, IQrCodeService qrCodeService)
    {
        _assets = assets;
        _qrCodeService = qrCodeService;
    }

    public async Task<Result<IReadOnlyList<AssetDto>>> Handle(
        GetAssetsByTagsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Tags.Count > MaxTags)
        {
            return Result.Failure<IReadOnlyList<AssetDto>>(
                AppErrors.BadRequest($"At most {MaxTags} tags can be requested at once."));
        }

        var assets = await _assets.GetByTagsAsync(request.Tags, cancellationToken);

        var dtos = assets
            .Select(asset => AssetDto.FromEntity(asset, _qrCodeService.GenerateQrCodeBase64(asset.AssetTag)))
            .ToList();

        return Result.Success<IReadOnlyList<AssetDto>>(dtos);
    }
}
