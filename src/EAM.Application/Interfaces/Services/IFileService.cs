namespace EAM.Application.Interfaces.Services;

/// <summary>CSV/Excel read &amp; write helper for import/export features.</summary>
public interface IFileService
{
    // ── Export ──
    Task<byte[]> WriteToExcelAsync<T>(IEnumerable<T> records, string sheetName = "Data", CancellationToken ct = default);
    Task<byte[]> WriteToCsvAsync<T>(IEnumerable<T> records, CancellationToken ct = default);
    Task<byte[]> WriteDynamicToExcelAsync(IEnumerable<Dictionary<string, object>> records, string sheetName = "Data", CancellationToken ct = default);
    Task<byte[]> WriteDynamicToCsvAsync(IEnumerable<Dictionary<string, object>> records, CancellationToken ct = default);

    // ── Import ──
    Task<List<T>> ReadFromCsvAsync<T>(Stream fileStream, CancellationToken ct = default);
    Task<List<T>> ReadFromExcelAsync<T>(Stream fileStream, CancellationToken ct = default);
}
