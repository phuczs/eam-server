namespace EAM.Application.Common;

/// <summary>
/// Packages raw file bytes plus the HTTP attributes needed to stream a download
/// (used by import/export features once they are built).
/// </summary>
public class ExportResult
{
    /// <summary>The raw byte payload of the compiled file (Excel or ZIP binary).</summary>
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();

    /// <summary>The MIME media type (e.g. the spreadsheet or zip content type).</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>The evaluated file name including extension.</summary>
    public string FileName { get; set; } = string.Empty;
}
