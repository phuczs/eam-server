namespace EAM.Domain.Entities;

public class AccountTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string TransactionNo { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string TransactionType { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal? BalanceAfter { get; set; }

    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Description { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TopupItem> TopupItems { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}
