using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Maintenance;
using AssetTagBD.Domain.Enums;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Maintenance;

public sealed record ResolveMaintenanceLogCommand(Guid Id) : IRequest<Result<MaintenanceLogDto>>;

public sealed class ResolveMaintenanceLogCommandValidator : AbstractValidator<ResolveMaintenanceLogCommand>
{
    public ResolveMaintenanceLogCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class ResolveMaintenanceLogCommandHandler
    : IRequestHandler<ResolveMaintenanceLogCommand, Result<MaintenanceLogDto>>
{
    private readonly IMaintenanceLogRepository _maintenanceLogs;
    private readonly IAssetRepository _assets;
    private readonly IAssignmentRepository _assignments;
    private readonly IUnitOfWork _unitOfWork;

    public ResolveMaintenanceLogCommandHandler(
        IMaintenanceLogRepository maintenanceLogs,
        IAssetRepository assets,
        IAssignmentRepository assignments,
        IUnitOfWork unitOfWork)
    {
        _maintenanceLogs = maintenanceLogs;
        _assets = assets;
        _assignments = assignments;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MaintenanceLogDto>> Handle(
        ResolveMaintenanceLogCommand request,
        CancellationToken cancellationToken)
    {
        var log = await _maintenanceLogs.GetByIdWithDetailsAsync(request.Id, cancellationToken);

        if (log is null)
        {
            return Result.Failure<MaintenanceLogDto>(AppErrors.NotFound("Maintenance log", request.Id));
        }

        if (log.Status == MaintenanceStatus.Resolved)
        {
            return Result.Failure<MaintenanceLogDto>(AppErrors.Conflict("This maintenance log is already resolved."));
        }

        log.Status = MaintenanceStatus.Resolved;
        log.ResolvedAt = DateTime.UtcNow;

        // Return the asset to service only when it has no other open tickets.
        var asset = await _assets.GetByIdAsync(log.AssetId, cancellationToken);
        if (asset is not null && asset.Status == AssetStatus.UnderMaintenance)
        {
            var openTickets = await _maintenanceLogs.GetByAssetIdAsync(asset.Id, cancellationToken);
            if (openTickets.All(m => m.Status == MaintenanceStatus.Resolved || m.Id == log.Id))
            {
                var hasActiveAssignment = await _assignments.GetActiveByAssetIdAsync(asset.Id, cancellationToken);
                asset.Status = hasActiveAssignment is null ? AssetStatus.Available : AssetStatus.Assigned;
                _assets.Update(asset);
            }
        }

        _maintenanceLogs.Update(log);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MaintenanceLogDto.FromEntity(log));
    }
}
