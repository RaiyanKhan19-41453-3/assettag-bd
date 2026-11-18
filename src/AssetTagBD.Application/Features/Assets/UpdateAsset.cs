using AssetTagBD.Application.Common.Extensions;
using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Assets;
using AssetTagBD.Domain.Enums;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Assets;

public sealed record UpdateAssetCommand(
    Guid Id,
    string Name,
    AssetCategory Category,
    string? SerialNumber,
    DateTime PurchaseDate,
    DateTime? WarrantyExpiryDate,
    AssetStatus Status) : IRequest<Result<AssetDto>>;

public sealed class UpdateAssetCommandValidator : AbstractValidator<UpdateAssetCommand>
{
    public UpdateAssetCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.PurchaseDate).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.WarrantyExpiryDate)
            .GreaterThan(x => x.PurchaseDate)
            .When(x => x.WarrantyExpiryDate.HasValue)
            .WithMessage("Warranty expiry date must be after the purchase date.");
    }
}

public sealed class UpdateAssetCommandHandler : IRequestHandler<UpdateAssetCommand, Result<AssetDto>>
{
    private readonly IAssetRepository _assets;
    private readonly IAssignmentRepository _assignments;
    private readonly IMaintenanceLogRepository _maintenanceLogs;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAssetCommandHandler(
        IAssetRepository assets,
        IAssignmentRepository assignments,
        IMaintenanceLogRepository maintenanceLogs,
        IUnitOfWork unitOfWork)
    {
        _assets = assets;
        _assignments = assignments;
        _maintenanceLogs = maintenanceLogs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AssetDto>> Handle(UpdateAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _assets.GetByIdWithDetailsAsync(request.Id, cancellationToken);

        if (asset is null)
        {
            return Result.Failure<AssetDto>(AppErrors.NotFound("Asset", request.Id));
        }

        // Status must stay consistent with the assignment record. Editing it
        // directly would leave the asset unassignable or unreturnable.
        var activeAssignment = await _assignments.GetActiveByAssetIdAsync(asset.Id, cancellationToken);
        if (request.Status == AssetStatus.Assigned && activeAssignment is null)
        {
            return Result.Failure<AssetDto>(AppErrors.Conflict(
                "Use the assign workflow to mark an asset as Assigned."));
        }

        if (request.Status == AssetStatus.Available && activeAssignment is not null)
        {
            return Result.Failure<AssetDto>(AppErrors.Conflict(
                "Return the asset before marking it Available."));
        }

        if (request.Status == AssetStatus.Retired && activeAssignment is not null)
        {
            return Result.Failure<AssetDto>(AppErrors.Conflict(
                "Return the asset before retiring it."));
        }

        if (request.Status == AssetStatus.UnderMaintenance)
        {
            var logs = await _maintenanceLogs.GetByAssetIdAsync(asset.Id, cancellationToken);
            if (!logs.Any(log => log.Status is MaintenanceStatus.Reported or MaintenanceStatus.InProgress))
            {
                return Result.Failure<AssetDto>(AppErrors.Conflict(
                    "Report a maintenance issue before marking the asset UnderMaintenance."));
            }
        }

        asset.Name = request.Name.Trim();
        asset.Category = request.Category;
        asset.SerialNumber = string.IsNullOrWhiteSpace(request.SerialNumber) ? null : request.SerialNumber.Trim();
        asset.PurchaseDate = request.PurchaseDate.AsUtc();
        asset.WarrantyExpiryDate = request.WarrantyExpiryDate.AsUtc();
        asset.Status = request.Status;

        _assets.Update(asset);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(AssetDto.FromEntity(asset));
    }
}
