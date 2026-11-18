using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Maintenance;
using AssetTagBD.Domain.Entities;
using AssetTagBD.Domain.Enums;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Maintenance;

public sealed record CreateMaintenanceLogCommand(Guid AssetId, string IssueDescription)
    : IRequest<Result<MaintenanceLogDto>>;

public sealed class CreateMaintenanceLogCommandValidator : AbstractValidator<CreateMaintenanceLogCommand>
{
    public CreateMaintenanceLogCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.IssueDescription)
            .NotEmpty().WithMessage("Issue description is required.")
            .MaximumLength(2000);
    }
}

public sealed class CreateMaintenanceLogCommandHandler
    : IRequestHandler<CreateMaintenanceLogCommand, Result<MaintenanceLogDto>>
{
    private readonly IAssetRepository _assets;
    private readonly IUserRepository _users;
    private readonly IMaintenanceLogRepository _maintenanceLogs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateMaintenanceLogCommandHandler(
        IAssetRepository assets,
        IUserRepository users,
        IMaintenanceLogRepository maintenanceLogs,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _assets = assets;
        _users = users;
        _maintenanceLogs = maintenanceLogs;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<MaintenanceLogDto>> Handle(
        CreateMaintenanceLogCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Result.Failure<MaintenanceLogDto>(AppErrors.Unauthorized("Authentication is required."));
        }

        var asset = await _assets.GetByIdAsync(request.AssetId, cancellationToken);
        if (asset is null)
        {
            return Result.Failure<MaintenanceLogDto>(AppErrors.NotFound("Asset", request.AssetId));
        }

        if (asset.Status == AssetStatus.Retired)
        {
            return Result.Failure<MaintenanceLogDto>(
                AppErrors.Conflict("Maintenance cannot be reported for a retired asset."));
        }

        var reporter = await _users.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (reporter is null)
        {
            return Result.Failure<MaintenanceLogDto>(AppErrors.Unauthorized("The current user no longer exists."));
        }

        var log = new MaintenanceLog
        {
            AssetId = asset.Id,
            Asset = asset,
            ReportedByUserId = reporter.Id,
            ReportedByUser = reporter,
            IssueDescription = request.IssueDescription.Trim(),
            Status = MaintenanceStatus.Reported,
            TenantId = asset.TenantId
        };

        asset.Status = AssetStatus.UnderMaintenance;

        await _maintenanceLogs.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MaintenanceLogDto.FromEntity(log));
    }
}
