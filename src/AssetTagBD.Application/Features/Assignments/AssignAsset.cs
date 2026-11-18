using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Assignments;
using AssetTagBD.Domain.Entities;
using AssetTagBD.Domain.Enums;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Assignments;

public sealed record AssignAssetCommand(
    Guid AssetId,
    Guid AssignedToUserId,
    string? ConditionNotes) : IRequest<Result<AssignmentDto>>;

public sealed class AssignAssetCommandValidator : AbstractValidator<AssignAssetCommand>
{
    public AssignAssetCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.AssignedToUserId).NotEmpty();
        RuleFor(x => x.ConditionNotes).MaximumLength(1000);
    }
}

public sealed class AssignAssetCommandHandler : IRequestHandler<AssignAssetCommand, Result<AssignmentDto>>
{
    private readonly IAssetRepository _assets;
    private readonly IUserRepository _users;
    private readonly IAssignmentRepository _assignments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AssignAssetCommandHandler(
        IAssetRepository assets,
        IUserRepository users,
        IAssignmentRepository assignments,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _assets = assets;
        _users = users;
        _assignments = assignments;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<AssignmentDto>> Handle(AssignAssetCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Result.Failure<AssignmentDto>(AppErrors.Unauthorized("Authentication is required."));
        }

        var asset = await _assets.GetByIdAsync(request.AssetId, cancellationToken);
        if (asset is null)
        {
            return Result.Failure<AssignmentDto>(AppErrors.NotFound("Asset", request.AssetId));
        }

        if (asset.Status != AssetStatus.Available)
        {
            return Result.Failure<AssignmentDto>(
                AppErrors.Conflict("Only an available asset can be assigned."));
        }

        var assignee = await _users.GetByIdAsync(request.AssignedToUserId, cancellationToken);
        if (assignee is null)
        {
            return Result.Failure<AssignmentDto>(AppErrors.NotFound("User", request.AssignedToUserId));
        }

        if (assignee.Role != UserRole.Employee)
        {
            return Result.Failure<AssignmentDto>(
                AppErrors.Conflict("Assets can only be assigned to employees."));
        }

        var assigner = await _users.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (assigner is null)
        {
            return Result.Failure<AssignmentDto>(AppErrors.Unauthorized("The current user no longer exists."));
        }

        var assignment = new Assignment
        {
            AssetId = asset.Id,
            Asset = asset,
            AssignedToUserId = assignee.Id,
            AssignedToUser = assignee,
            AssignedByUserId = assigner.Id,
            AssignedByUser = assigner,
            AssignedDate = DateTime.UtcNow,
            ConditionNotes = string.IsNullOrWhiteSpace(request.ConditionNotes) ? null : request.ConditionNotes.Trim(),
            TenantId = asset.TenantId
        };

        asset.Status = AssetStatus.Assigned;

        await _assignments.AddAsync(assignment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(AssignmentDto.FromEntity(assignment));
    }
}
