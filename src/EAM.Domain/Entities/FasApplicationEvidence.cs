namespace EAM.Domain.Entities;

public class FasApplicationEvidence
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ApplicationId { get; set; }
    public FasApplication Application { get; set; } = null!;

    public string EvidenceType { get; set; } = null!;
    public string? Title { get; set; }
    public string? Description { get; set; }

    public string FileName { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public string? MimeType { get; set; }
    public long? FileSizeBytes { get; set; }

    public string Status { get; set; } = "uploaded";
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewRemarks { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
