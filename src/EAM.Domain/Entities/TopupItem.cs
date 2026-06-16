namespace EAM.Domain.Entities;

public class TopupItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BatchId { get; set; }
    public TopupBatch Batch { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Status { get; set; } = "draft";

    public Guid? TransactionId { get; set; }
    public AccountTransaction? Transaction { get; set; }

    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
