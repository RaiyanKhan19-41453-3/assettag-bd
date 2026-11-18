using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Assignments;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Assignments;

public sealed record GetAssignmentHistoryQuery(Guid AssetId) : IRequest<Result<IReadOnlyList<AssignmentDto>>>;

public sealed class GetAssignmentHistoryQueryValidator : AbstractValidator<GetAssignmentHistoryQuery>
{
    public GetAssignmentHistoryQueryValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
    }
}

public sealed class GetAssignmentHistoryQueryHandler
    : IRequestHandler<GetAssignmentHistoryQuery, Result<IReadOnlyList<AssignmentDto>>>
{
    private readonly IAssignmentRepository _assignments;

    public GetAssignmentHistoryQueryHandler(IAssignmentRepository assignments)
    {
        _assignments = assignments;
    }

    public async Task<Result<IReadOnlyList<AssignmentDto>>> Handle(
        GetAssignmentHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var history = await _assignments.GetHistoryByAssetIdAsync(request.AssetId, cancellationToken);

        var dtos = history
            .OrderByDescending(a => a.AssignedDate)
            .Select(AssignmentDto.FromEntity)
            .ToList();

        return Result.Success<IReadOnlyList<AssignmentDto>>(dtos);
    }
}
