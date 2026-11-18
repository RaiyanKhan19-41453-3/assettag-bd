using System.Globalization;
using AssetTagBD.Application.Common.Extensions;
using AssetTagBD.Application.Common.Interfaces;
using AssetTagBD.Application.Common.Models;
using AssetTagBD.Application.DTOs.Assets;
using AssetTagBD.Domain.Entities;
using AssetTagBD.Domain.Enums;
using FluentValidation;
using MediatR;

namespace AssetTagBD.Application.Features.Assets;

public sealed record ImportAssetsCommand(byte[] FileContent, string FileName)
    : IRequest<Result<ImportAssetsResult>>;

public sealed class ImportAssetsCommandValidator : AbstractValidator<ImportAssetsCommand>
{
    private const int MaxFileSize = 5 * 1024 * 1024;

    public ImportAssetsCommandValidator()
    {
        RuleFor(x => x.FileContent)
            .NotEmpty().WithMessage("Please choose a spreadsheet file.")
            .Must(content => content.Length <= MaxFileSize)
            .WithMessage("The spreadsheet must be smaller than 5 MB.");

        RuleFor(x => x.FileName)
            .Must(name => name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                          name.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .xlsx and .xlsm spreadsheets are supported.");
    }
}

public sealed class ImportAssetsCommandHandler : IRequestHandler<ImportAssetsCommand, Result<ImportAssetsResult>>
{
    private readonly IExcelAssetReader _excelReader;
    private readonly IAssetRepository _assets;
    private readonly IAssetTagGenerator _tagGenerator;
    private readonly IQrCodeService _qrCodeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ImportAssetsCommandHandler(
        IExcelAssetReader excelReader,
        IAssetRepository assets,
        IAssetTagGenerator tagGenerator,
        IQrCodeService qrCodeService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _excelReader = excelReader;
        _assets = assets;
        _tagGenerator = tagGenerator;
        _qrCodeService = qrCodeService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<ImportAssetsResult>> Handle(
        ImportAssetsCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Result.Failure<ImportAssetsResult>(AppErrors.Unauthorized("An active tenant is required."));
        }

        IReadOnlyList<AssetImportRow> rows;
        try
        {
            rows = _excelReader.Read(request.FileContent);
        }
        catch (InvalidDataException exception)
        {
            return Result.Failure<ImportAssetsResult>(AppErrors.BadRequest(exception.Message));
        }

        var errors = new List<string>();
        var validAssets = new List<Asset>();
        var tagsInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var rowErrors = new List<string>();

            if (row.Name.Length > 200) rowErrors.Add("Name is longer than 200 characters.");
            if (row.SerialNumber is { Length: > 100 }) rowErrors.Add("Serial number is longer than 100 characters.");

            var category = AssetCategory.Other;
            if (!string.IsNullOrWhiteSpace(row.Category) &&
                !Enum.TryParse(row.Category, ignoreCase: true, out category))
            {
                rowErrors.Add($"Unknown category '{row.Category}'.");
            }

            if (!TryParseDate(row.PurchaseDate, out var purchaseDate))
            {
                rowErrors.Add("Purchase date is missing or invalid.");
            }

            DateTime? warranty = null;
            if (!string.IsNullOrWhiteSpace(row.WarrantyExpiryDate))
            {
                if (!TryParseDate(row.WarrantyExpiryDate, out var parsedWarranty))
                {
                    rowErrors.Add("Warranty expiry date is invalid.");
                }
                else if (parsedWarranty <= purchaseDate)
                {
                    rowErrors.Add("Warranty expiry must be after the purchase date.");
                }
                else
                {
                    warranty = parsedWarranty.AsUtc();
                }
            }

            var assetTag = string.IsNullOrWhiteSpace(row.AssetTag)
                ? await _tagGenerator.GenerateAsync(category, cancellationToken)
                : row.AssetTag.Trim().ToUpperInvariant();

            if (assetTag.Length > 100) rowErrors.Add("Asset tag is longer than 100 characters.");
            if (!tagsInFile.Add(assetTag) || await _assets.TagExistsAsync(assetTag, cancellationToken))
            {
                rowErrors.Add($"Asset tag '{assetTag}' is duplicated or already in use.");
            }

            if (rowErrors.Count > 0)
            {
                errors.Add($"Row {row.RowNumber}: {string.Join(" ", rowErrors)}");
                continue;
            }

            validAssets.Add(new Asset
            {
                AssetTag = assetTag,
                Name = row.Name.Trim(),
                Category = category,
                SerialNumber = string.IsNullOrWhiteSpace(row.SerialNumber) ? null : row.SerialNumber.Trim(),
                PurchaseDate = purchaseDate.AsUtc(),
                WarrantyExpiryDate = warranty,
                Status = AssetStatus.Available,
                TenantId = _currentUser.TenantId.Value
            });
        }

        if (validAssets.Count > 0)
        {
            await _assets.AddRangeAsync(validAssets, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var dtos = validAssets
            .Select(asset => AssetDto.FromEntity(asset, _qrCodeService.GenerateQrCodeBase64(asset.AssetTag)))
            .ToList();

        var result = new ImportAssetsResult(validAssets.Count, errors.Count, errors, dtos);
        return Result.Success(result);
    }

    private static bool TryParseDate(string? value, out DateTime date)
    {
        date = default;
        return !string.IsNullOrWhiteSpace(value) &&
               DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
