using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using EAM.Application.Interfaces.Services;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace EAM.Infrastructure.Helper;

/// <summary>CSV/Excel read &amp; write helper (ClosedXML + CsvHelper) for import/export.</summary>
public class FileService : IFileService
{
    // ── Export (write) ──
    public async Task<byte[]> WriteToCsvAsync<T>(IEnumerable<T> records, CancellationToken ct = default)
    {
        using var memoryStream = new MemoryStream();

        // Emits a UTF-8 BOM so Excel maps special characters correctly.
        await using (var writer = new StreamWriter(memoryStream, new UTF8Encoding(true)))
        await using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true }))
        {
            await csv.WriteRecordsAsync(records, ct);
            await writer.FlushAsync(ct);
        }
        return memoryStream.ToArray();
    }

    public Task<byte[]> WriteToExcelAsync<T>(IEnumerable<T> records, string sheetName = "Data", CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Reflection table insertion parses property headers automatically.
        worksheet.Cell(1, 1).InsertTable(records);
        worksheet.Columns().AdjustToContents();

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return Task.FromResult(memoryStream.ToArray());
    }

    // ── Import (read) ──
    public async Task<List<T>> ReadFromCsvAsync<T>(Stream fileStream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(fileStream, Encoding.UTF8);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            HeaderValidated = null,
            MissingFieldFound = null
        });

        var list = new List<T>();
        await foreach (var record in csv.GetRecordsAsync<T>(ct))
            list.Add(record);
        return list;
    }

    public Task<List<T>> ReadFromExcelAsync<T>(Stream fileStream, CancellationToken ct = default)
    {
        var list = new List<T>();

        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("The provided Excel workbook contains no valid worksheets.");

        var rows = worksheet.RangeUsed()?.RowsUsed();
        if (rows is null) return Task.FromResult(list);

        var headerRow = rows.First();
        var dataRows = rows.Skip(1);

        // Map column headers to their column index.
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i <= headerRow.LastCellUsed()!.Address.ColumnNumber; i++)
        {
            var headerName = headerRow.Cell(i).GetString().Trim();
            if (!string.IsNullOrEmpty(headerName))
                headerMap[headerName] = i;
        }

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var row in dataRows)
        {
            if (ct.IsCancellationRequested) break;

            var item = Activator.CreateInstance<T>()
                ?? throw new InvalidOperationException($"Failed to initialize type {typeof(T).Name}");

            foreach (var prop in properties)
            {
                if (!headerMap.TryGetValue(prop.Name, out int colIndex)) continue;

                var cellValue = row.Cell(colIndex).GetString().Trim();
                if (string.IsNullOrEmpty(cellValue)) continue;

                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                object? convertedValue;

                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = targetType.GetGenericArguments()[0];
                    var listInstance = (System.Collections.IList)Activator.CreateInstance(targetType)!;
                    var items = cellValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var strItem in items)
                    {
                        var trimmed = strItem.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                            listInstance.Add(Convert.ChangeType(trimmed, elementType));
                    }
                    convertedValue = listInstance;
                }
                else if (targetType == typeof(Guid))
                {
                    convertedValue = Guid.Parse(cellValue);
                }
                else if (targetType.IsEnum)
                {
                    convertedValue = Enum.Parse(targetType, cellValue, true);
                }
                else
                {
                    convertedValue = Convert.ChangeType(cellValue, targetType);
                }

                prop.SetValue(item, convertedValue);
            }

            list.Add(item);
        }

        return Task.FromResult(list);
    }

    // ── Dynamic export ──
    public Task<byte[]> WriteDynamicToExcelAsync(IEnumerable<Dictionary<string, object>> records, string sheetName = "Data", CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        if (records != null && records.Any())
        {
            var dataTable = new DataTable();
            foreach (var key in records.First().Keys)
                dataTable.Columns.Add(key);

            foreach (var record in records)
            {
                if (ct.IsCancellationRequested) break;
                var row = dataTable.NewRow();
                foreach (var key in record.Keys)
                    row[key] = record[key] ?? DBNull.Value;
                dataTable.Rows.Add(row);
            }

            worksheet.Cell(1, 1).InsertTable(dataTable);
            worksheet.Columns().AdjustToContents();
        }

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return Task.FromResult(memoryStream.ToArray());
    }

    public async Task<byte[]> WriteDynamicToCsvAsync(IEnumerable<Dictionary<string, object>> records, CancellationToken ct = default)
    {
        using var memoryStream = new MemoryStream();
        await using (var writer = new StreamWriter(memoryStream, new UTF8Encoding(true)))
        await using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            if (records != null && records.Any())
            {
                foreach (var header in records.First().Keys)
                    csv.WriteField(header);
                await csv.NextRecordAsync();

                foreach (var record in records)
                {
                    if (ct.IsCancellationRequested) break;
                    foreach (var value in record.Values)
                        csv.WriteField(value);
                    await csv.NextRecordAsync();
                }
            }
            await writer.FlushAsync(ct);
        }
        return memoryStream.ToArray();
    }
}
