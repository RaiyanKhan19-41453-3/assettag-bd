using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Audit;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Audit;

public sealed record GetAuditEntriesQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<AuditEntryDto>>>;

public sealed class GetAuditEntriesQueryValidator : AbstractValidator<GetAuditEntriesQuery>
{
    public GetAuditEntriesQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 100_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetAuditEntriesQueryHandler
    : IRequestHandler<GetAuditEntriesQuery, Result<PagedResult<AuditEntryDto>>>
{
    private readonly IAuditRepository _audit;

    public GetAuditEntriesQueryHandler(IAuditRepository audit)
    {
        _audit = audit;
    }

    public async Task<Result<PagedResult<AuditEntryDto>>> Handle(
        GetAuditEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _audit.SearchAsync(request.PageNumber, request.PageSize, cancellationToken);

        var dtos = page.Items.Select(entry => new AuditEntryDto(
            entry.Id,
            entry.Action,
            entry.EntityType,
            entry.EntityId,
            entry.Actor,
            entry.Details,
            entry.OccurredAt)).ToList();

        return Result.Success(PagedResult<AuditEntryDto>.Create(
            dtos, page.PageNumber, page.PageSize, page.TotalCount));
    }
}
