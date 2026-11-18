using System.Globalization;
using AssetTagBD.Application.Common.Interfaces;
using ClosedXML.Excel;

namespace AssetTagBD.Infrastructure.Services;

/// <summary>
/// Reads the first worksheet of an .xlsx file. Column headers are matched by
/// normalized name so clients can export from any spreadsheet tool.
/// </summary>
public sealed class ExcelAssetReader : IExcelAssetReader
{
    private static readonly Dictionary<string, string> ColumnAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = "name",
        ["assetname"] = "name",
        ["asset"] = "name",
        ["category"] = "category",
        ["type"] = "category",
        ["assettag"] = "assetTag",
        ["tag"] = "assetTag",
        ["serialnumber"] = "serialNumber",
        ["serial"] = "serialNumber",
        ["purchasedate"] = "purchaseDate",
        ["purchase"] = "purchaseDate",
        ["warrantyexpirydate"] = "warrantyExpiryDate",
        ["warrantyexpiry"] = "warrantyExpiryDate",
        ["warranty"] = "warrantyExpiryDate"
    };

    public IReadOnlyList<AssetImportRow> Read(byte[] fileContent)
    {
        if (fileContent is null || fileContent.Length == 0)
        {
            throw new InvalidDataException("The uploaded file is empty.");
        }

        using var stream = new MemoryStream(fileContent);

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception exception) when (exception is not InvalidDataException)
        {
            throw new InvalidDataException("The file could not be read as an .xlsx spreadsheet.");
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidDataException("The spreadsheet has no worksheets.");

            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in sheet.Row(1).CellsUsed())
            {
                var header = Normalize(cell.GetString());
                if (ColumnAliases.TryGetValue(header, out var field))
                {
                    columns[field] = cell.Address.ColumnNumber;
                }
            }

            if (!columns.ContainsKey("name"))
            {
                throw new InvalidDataException(
                    "The spreadsheet must have a column named 'Name' (or 'Asset Name').");
            }

            var rows = new List<AssetImportRow>();
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var row = sheet.Row(rowNumber);
                var name = GetCellText(row, columns, "name") ?? string.Empty;

                // Ignore completely blank rows (common in exported sheets).
                if (string.IsNullOrWhiteSpace(name)) continue;

                rows.Add(new AssetImportRow(
                    rowNumber,
                    GetCellText(row, columns, "assetTag"),
                    name.Trim(),
                    GetCellText(row, columns, "category"),
                    GetCellText(row, columns, "serialNumber"),
                    GetCellText(row, columns, "purchaseDate"),
                    GetCellText(row, columns, "warrantyExpiryDate")));
            }

            return rows;
        }
    }

    private static string Normalize(string value) =>
        new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static string? GetCellText(IXLRow row, Dictionary<string, int> columns, string field)
    {
        if (!columns.TryGetValue(field, out var column)) return null;

        var cell = row.Cell(column);
        if (cell.IsEmpty()) return null;

        if (cell.DataType == XLDataType.DateTime && cell.TryGetValue<DateTime>(out var date))
        {
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return cell.GetString().Trim();
    }
}
