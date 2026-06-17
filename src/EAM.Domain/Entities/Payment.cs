namespace EAM.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string PaymentNo { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;
    public string Status { get; set; } = "pending";
    public decimal Amount { get; set; }

    public string? IdempotencyKey { get; set; }
    public string? GatewayName { get; set; }
    public string? ExternalPaymentRef { get; set; }
    public string? GatewayPayloadJson { get; set; }

    public Guid? AccountTransactionId { get; set; }
    public AccountTransaction? AccountTransaction { get; set; }

    public string? ReceiptNo { get; set; }
    public string? ReceiptFileUrl { get; set; }

    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? UserBankAccountId { get; set; }
    public UserBankAccount? UserBankAccount { get; set; }

    public ICollection<PaymentAllocation> Allocations { get; set; } = [];
}
