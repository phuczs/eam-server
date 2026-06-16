namespace EAM.Domain.Entities;

public class TopupBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string BatchNo { get; set; } = null!;
    public string BatchName { get; set; } = null!;
    public string SourceType { get; set; } = "manual";
    public string Status { get; set; } = "draft";
    public string? Reason { get; set; }

    public decimal TotalAmount { get; set; } = 0;
    public int TotalAccounts { get; set; } = 0;
    public int SuccessCount { get; set; } = 0;
    public int FailedCount { get; set; } = 0;

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    public ICollection<TopupItem> Items { get; set; } = [];
}
