using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Assets;
using AssetTagBD.Domain.Enums;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Assets;

public sealed record GetAssetsQuery(
    AssetCategory? Category,
    AssetStatus? Status,
    string? Search,
    int PageNumber = 1,
    int PageSize = 12,
    string? SortBy = null,
    bool Descending = false) : IRequest<Result<PagedResult<AssetDto>>>;

public sealed class GetAssetsQueryValidator : AbstractValidator<GetAssetsQuery>
{
    public GetAssetsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}

public sealed class GetAssetsQueryHandler : IRequestHandler<GetAssetsQuery, Result<PagedResult<AssetDto>>>
{
    private readonly IAssetRepository _assets;

    public GetAssetsQueryHandler(IAssetRepository assets)
    {
        _assets = assets;
    }

    public async Task<Result<PagedResult<AssetDto>>> Handle(
        GetAssetsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _assets.SearchAsync(
            request.Category,
            request.Status,
            request.Search,
            request.PageNumber,
            request.PageSize,
            request.SortBy,
            request.Descending,
            cancellationToken);

        var dtos = page.Items.Select(asset => AssetDto.FromEntity(asset)).ToList();

        return Result.Success(PagedResult<AssetDto>.Create(
            dtos, page.PageNumber, page.PageSize, page.TotalCount));
    }
}
