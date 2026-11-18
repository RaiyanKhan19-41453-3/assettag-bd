using AssetTagBD.Application.Features.Assets;
using AssetTagBD.Domain.Enums;
using ClosedXML.Excel;

namespace AssetTagBD.Integration.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class ImportTests : IntegrationTestBase
{
    public ImportTests(TestFixture fixture) : base(fixture)
    {
    }

    private async Task<(Guid TenantId, Guid AdminId, string Email)> ArrangeAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"import-{suffix}@test.local";
        var (tenantId, adminId) = await SeedTenantAsync($"Import Co {suffix}", $"import-{suffix}", email);
        ActAs(adminId, tenantId, UserRole.SuperAdmin, email);
        return (tenantId, adminId, email);
    }

    private static byte[] BuildWorkbook(params (string Name, string? Category, string? Tag, string? Purchase)[] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Assets");
        sheet.Cell(1, 1).Value = "Name";
        sheet.Cell(1, 2).Value = "Category";
        sheet.Cell(1, 3).Value = "Asset Tag";
        sheet.Cell(1, 4).Value = "Purchase Date";

        for (var i = 0; i < rows.Length; i++)
        {
            sheet.Cell(i + 2, 1).Value = rows[i].Name;
            if (rows[i].Category is not null) sheet.Cell(i + 2, 2).Value = rows[i].Category;
            if (rows[i].Tag is not null) sheet.Cell(i + 2, 3).Value = rows[i].Tag;
            if (rows[i].Purchase is not null) sheet.Cell(i + 2, 4).Value = rows[i].Purchase;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    [Fact]
    public async Task Import_ValidRows_ArePersistedWithQrCodes()
    {
        await ArrangeAsync();
        var before = await CountAssetsAsync();

        var file = BuildWorkbook(
            ("Imported Laptop", "Laptop", null, "2025-01-15"),
            ("Imported Chair", "Furniture", "CUSTOM-TAG-001", "2024-05-01"));

        var result = await Send(new ImportAssetsCommand(file, "assets.xlsx"));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Created);
        Assert.Equal(0, result.Value.Skipped);
        Assert.Equal(before + 2, await CountAssetsAsync());
        Assert.All(result.Value.Assets, asset => Assert.False(string.IsNullOrWhiteSpace(asset.QrCodeBase64)));
        Assert.Contains(result.Value.Assets, asset => asset.AssetTag == "CUSTOM-TAG-001");
    }

    [Fact]
    public async Task Import_InvalidRows_AreSkippedWithRowErrors()
    {
        await ArrangeAsync();

        var file = BuildWorkbook(
            ("Good Asset", "Laptop", null, "2025-01-15"),
            ("Bad Category", "NotACategory", null, "2025-01-15"),
            ("Bad Date", "Laptop", null, "not-a-date"),
            ("Duplicate Tag", "Laptop", "CUSTOM-TAG-001", "2025-01-15"),
            ("Duplicate Again", "Laptop", "CUSTOM-TAG-001", "2025-01-15"));

        var result = await Send(new ImportAssetsCommand(file, "assets.xlsx"));

        Assert.True(result.IsSuccess);
        // Row 2 is valid; row 5 is the first use of CUSTOM-TAG-001 and is also valid.
        // Rows 3, 4 and 6 fail (category, date, duplicate tag).
        Assert.Equal(2, result.Value!.Created);
        Assert.Equal(3, result.Value.Skipped);
        Assert.Contains(result.Value.Errors, error => error.Contains("Row 3"));
        Assert.Contains(result.Value.Errors, error => error.Contains("Row 4"));
        Assert.Contains(result.Value.Errors, error => error.Contains("duplicated"));
    }

    [Fact]
    public async Task Import_WithoutNameColumn_ReturnsBadRequest()
    {
        await ArrangeAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Assets");
        sheet.Cell(1, 1).Value = "Something Else";
        sheet.Cell(2, 1).Value = "Value";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var result = await Send(new ImportAssetsCommand(stream.ToArray(), "assets.xlsx"));

        Assert.True(result.IsFailure);
        Assert.Equal("BadRequest", result.Error.Code);
    }
}
