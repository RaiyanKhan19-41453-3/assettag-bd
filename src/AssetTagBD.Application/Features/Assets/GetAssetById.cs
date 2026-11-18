using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Assets;
using MediatR;

namespace AssetTagBD.Application.Features.Assets;

public sealed record GetAssetByIdQuery(Guid Id) : IRequest<Result<AssetDto>>;

public sealed class GetAssetByIdQueryHandler : IRequestHandler<GetAssetByIdQuery, Result<AssetDto>>
{
    private readonly IAssetRepository _assets;
    private readonly IQrCodeService _qrCodeService;

    public GetAssetByIdQueryHandler(IAssetRepository assets, IQrCodeService qrCodeService)
    {
        _assets = assets;
        _qrCodeService = qrCodeService;
    }

    public async Task<Result<AssetDto>> Handle(GetAssetByIdQuery request, CancellationToken cancellationToken)
    {
        var asset = await _assets.GetWithDetailsAsync(request.Id, cancellationToken);

        return asset is null
            ? Result.Failure<AssetDto>(AppErrors.NotFound("Asset", request.Id))
            : Result.Success(AssetDto.FromEntity(asset, _qrCodeService.GenerateQrCodeBase64(asset.AssetTag)));
    }
}
