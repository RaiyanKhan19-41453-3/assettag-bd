using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Assignments;
using AssetTagBD.Domain.Enums;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Assignments;

public sealed record ReturnAssetCommand(Guid AssetId, string? ConditionNotes) : IRequest<Result<AssignmentDto>>;

public sealed class ReturnAssetCommandValidator : AbstractValidator<ReturnAssetCommand>
{
    public ReturnAssetCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.ConditionNotes).MaximumLength(1000);
    }
}

public sealed class ReturnAssetCommandHandler : IRequestHandler<ReturnAssetCommand, Result<AssignmentDto>>
{
    private readonly IAssignmentRepository _assignments;
    private readonly IMaintenanceLogRepository _maintenanceLogs;
    private readonly IUnitOfWork _unitOfWork;

    public ReturnAssetCommandHandler(
        IAssignmentRepository assignments,
        IMaintenanceLogRepository maintenanceLogs,
        IUnitOfWork unitOfWork)
    {
        _assignments = assignments;
        _maintenanceLogs = maintenanceLogs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AssignmentDto>> Handle(ReturnAssetCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _assignments.GetActiveByAssetIdAsync(request.AssetId, cancellationToken);

        if (assignment is null)
        {
            return Result.Failure<AssignmentDto>(
                AppErrors.Conflict("This asset does not have an active assignment."));
        }

        assignment.ReturnedDate = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.ConditionNotes))
        {
            assignment.ConditionNotes = string.IsNullOrWhiteSpace(assignment.ConditionNotes)
                ? request.ConditionNotes.Trim()
                : $"{assignment.ConditionNotes} | Return: {request.ConditionNotes.Trim()}";
        }

        // Returning an asset must not silently clear a maintenance flag.
        var tickets = await _maintenanceLogs.GetByAssetIdAsync(request.AssetId, cancellationToken);
        assignment.Asset.Status = tickets.Any(t => t.Status != MaintenanceStatus.Resolved)
            ? AssetStatus.UnderMaintenance
            : AssetStatus.Available;

        _assignments.Update(assignment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(AssignmentDto.FromEntity(assignment));
    }
}
