namespace EAM.Domain.Entities;

public class PaymentAllocation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public decimal AllocatedAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
