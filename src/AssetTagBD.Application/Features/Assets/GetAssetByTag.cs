using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Assets;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Assets;

/// <summary>Used by the QR scanner: resolves a scanned tag straight to an asset.</summary>
public sealed record GetAssetByTagQuery(string AssetTag) : IRequest<Result<AssetDto>>;

public sealed class GetAssetByTagQueryValidator : AbstractValidator<GetAssetByTagQuery>
{
    public GetAssetByTagQueryValidator()
    {
        RuleFor(x => x.AssetTag).NotEmpty().MaximumLength(100);
    }
}

public sealed class GetAssetByTagQueryHandler : IRequestHandler<GetAssetByTagQuery, Result<AssetDto>>
{
    private readonly IAssetRepository _assets;
    private readonly IQrCodeService _qrCodeService;

    public GetAssetByTagQueryHandler(IAssetRepository assets, IQrCodeService qrCodeService)
    {
        _assets = assets;
        _qrCodeService = qrCodeService;
    }

    public async Task<Result<AssetDto>> Handle(GetAssetByTagQuery request, CancellationToken cancellationToken)
    {
        var asset = await _assets.GetByTagAsync(request.AssetTag.Trim().ToUpperInvariant(), cancellationToken);

        return asset is null
            ? Result.Failure<AssetDto>(AppErrors.NotFound("Asset", request.AssetTag))
            : Result.Success(AssetDto.FromEntity(asset, _qrCodeService.GenerateQrCodeBase64(asset.AssetTag)));
    }
}
