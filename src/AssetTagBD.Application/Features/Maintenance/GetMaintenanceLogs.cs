using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Maintenance;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Maintenance;

public sealed record GetMaintenanceLogsQuery(Guid AssetId) : IRequest<Result<IReadOnlyList<MaintenanceLogDto>>>;

public sealed class GetMaintenanceLogsQueryValidator : AbstractValidator<GetMaintenanceLogsQuery>
{
    public GetMaintenanceLogsQueryValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
    }
}

public sealed class GetMaintenanceLogsQueryHandler
    : IRequestHandler<GetMaintenanceLogsQuery, Result<IReadOnlyList<MaintenanceLogDto>>>
{
    private readonly IMaintenanceLogRepository _maintenanceLogs;

    public GetMaintenanceLogsQueryHandler(IMaintenanceLogRepository maintenanceLogs)
    {
        _maintenanceLogs = maintenanceLogs;
    }

    public async Task<Result<IReadOnlyList<MaintenanceLogDto>>> Handle(
        GetMaintenanceLogsQuery request,
        CancellationToken cancellationToken)
    {
        var logs = await _maintenanceLogs.GetByAssetIdAsync(request.AssetId, cancellationToken);

        var dtos = logs
            .OrderByDescending(m => m.CreatedAt)
            .Select(MaintenanceLogDto.FromEntity)
            .ToList();

        return Result.Success<IReadOnlyList<MaintenanceLogDto>>(dtos);
    }
}
