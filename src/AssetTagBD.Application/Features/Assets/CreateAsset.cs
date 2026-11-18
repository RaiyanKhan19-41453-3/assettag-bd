using AssetTagBD.Application.Common.Extensions;
using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Assets;
using AssetTagBD.Domain.Entities;
using AssetTagBD.Domain.Enums;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Assets;

public sealed record CreateAssetCommand(
    string? AssetTag,
    string Name,
    AssetCategory Category,
    string? SerialNumber,
    DateTime PurchaseDate,
    DateTime? WarrantyExpiryDate) : IRequest<Result<AssetDto>>;

public sealed class CreateAssetCommandValidator : AbstractValidator<CreateAssetCommand>
{
    public CreateAssetCommandValidator()
    {
        RuleFor(x => x.AssetTag).MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.PurchaseDate).NotEmpty();
        RuleFor(x => x.WarrantyExpiryDate)
            .GreaterThan(x => x.PurchaseDate)
            .When(x => x.WarrantyExpiryDate.HasValue)
            .WithMessage("Warranty expiry date must be after the purchase date.");
    }
}

public sealed class CreateAssetCommandHandler : IRequestHandler<CreateAssetCommand, Result<AssetDto>>
{
    private readonly IAssetRepository _assets;
    private readonly IQrCodeService _qrCodeService;
    private readonly IAssetTagGenerator _tagGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateAssetCommandHandler(
        IAssetRepository assets,
        IQrCodeService qrCodeService,
        IAssetTagGenerator tagGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _assets = assets;
        _qrCodeService = qrCodeService;
        _tagGenerator = tagGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<AssetDto>> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Result.Failure<AssetDto>(AppErrors.Unauthorized("An active tenant is required."));
        }

        var assetTag = string.IsNullOrWhiteSpace(request.AssetTag)
            ? await _tagGenerator.GenerateAsync(request.Category, cancellationToken)
            : request.AssetTag.Trim().ToUpperInvariant();

        if (await _assets.TagExistsAsync(assetTag, cancellationToken))
        {
            return Result.Failure<AssetDto>(AppErrors.Conflict($"Asset tag '{assetTag}' is already in use."));
        }

        var asset = new Asset
        {
            AssetTag = assetTag,
            Name = request.Name.Trim(),
            Category = request.Category,
            SerialNumber = string.IsNullOrWhiteSpace(request.SerialNumber) ? null : request.SerialNumber.Trim(),
            PurchaseDate = request.PurchaseDate.AsUtc(),
            WarrantyExpiryDate = request.WarrantyExpiryDate.AsUtc(),
            Status = AssetStatus.Available,
            TenantId = _currentUser.TenantId.Value
        };

        await _assets.AddAsync(asset, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var qrCode = _qrCodeService.GenerateQrCodeBase64(asset.AssetTag);

        return Result.Success(AssetDto.FromEntity(asset, qrCode));
    }
}
