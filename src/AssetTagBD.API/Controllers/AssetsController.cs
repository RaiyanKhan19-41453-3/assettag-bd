using AssetTagBD.API.Models.Requests;
using AssetTagBD.Application.Features.Assets;
using AssetTagBD.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetTagBD.API.Controllers;

[ApiController]
[Authorize]
public sealed class AssetsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AssetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] AssetCategory? category,
        [FromQuery] AssetStatus? status,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = false,
        CancellationToken cancellationToken = default) =>
        ToActionResult(await _mediator.Send(
            new GetAssetsQuery(category, status, search, pageNumber, pageSize, sortBy, descending),
            cancellationToken));

    /// <summary>Batch lookup used by the printable label sheet.</summary>
    [HttpPost("by-tags")]
    public async Task<IActionResult> GetByTags(
        [FromBody] GetAssetsByTagsRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await _mediator.Send(new GetAssetsByTagsQuery(request.Tags), cancellationToken));

    /// <summary>Assets currently assigned to the signed-in user.</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken) =>
        ToActionResult(await _mediator.Send(new GetMyAssetsQuery(), cancellationToken));

    [HttpGet("tag/{tag}")]
    public async Task<IActionResult> GetByTag(string tag, CancellationToken cancellationToken) =>
        ToActionResult(await _mediator.Send(new GetAssetByTagQuery(tag), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _mediator.Send(new GetAssetByIdQuery(id), cancellationToken));

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,ITManager")]
    public async Task<IActionResult> Create([FromBody] CreateAssetCommand command) =>
        ToActionResult(await _mediator.Send(command));

    /// <summary>Bulk import from an .xlsx spreadsheet (see README for the column format).</summary>
    [HttpPost("import")]
    [Authorize(Roles = "SuperAdmin,ITManager")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return ToActionResult(await _mediator.Send(
            new ImportAssetsCommand(stream.ToArray(), file.FileName), cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,ITManager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAssetRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _mediator.Send(
            new UpdateAssetCommand(
                id,
                request.Name,
                request.Category,
                request.SerialNumber,
                request.PurchaseDate,
                request.WarrantyExpiryDate,
                request.Status),
            cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _mediator.Send(new DeleteAssetCommand(id), cancellationToken));
}
