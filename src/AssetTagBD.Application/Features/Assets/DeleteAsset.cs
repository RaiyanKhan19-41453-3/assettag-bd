using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Assets;

public sealed record DeleteAssetCommand(Guid Id) : IRequest<Result<bool>>;

public sealed class DeleteAssetCommandValidator : AbstractValidator<DeleteAssetCommand>
{
    public DeleteAssetCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeleteAssetCommandHandler : IRequestHandler<DeleteAssetCommand, Result<bool>>
{
    private readonly IAssetRepository _assets;
    private readonly IAssignmentRepository _assignments;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAssetCommandHandler(
        IAssetRepository assets,
        IAssignmentRepository assignments,
        IUnitOfWork unitOfWork)
    {
        _assets = assets;
        _assignments = assignments;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _assets.GetByIdAsync(request.Id, cancellationToken);

        if (asset is null)
        {
            return Result.Failure<bool>(AppErrors.NotFound("Asset", request.Id));
        }

        var activeAssignment = await _assignments.GetActiveByAssetIdAsync(asset.Id, cancellationToken);
        if (activeAssignment is not null)
        {
            return Result.Failure<bool>(AppErrors.Conflict(
                "Return the asset before deleting it so the audit trail is preserved."));
        }

        // Soft delete: the asset disappears from every filtered query, but its
        // assignment and maintenance history is preserved for the audit trail.
        asset.DeletedAt = DateTime.UtcNow;
        _assets.Update(asset);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
