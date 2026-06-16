namespace EAM.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid BillingPeriodId { get; set; }
    public BillingPeriod BillingPeriod { get; set; } = null!;

    public string InvoiceNo { get; set; } = null!;
    public string Status { get; set; } = "draft";

    public decimal SubtotalAmount { get; set; } = 0;
    public decimal GstAmount { get; set; } = 0;
    public decimal SubsidyAmount { get; set; } = 0;
    public decimal WaiverAmount { get; set; } = 0;
    public decimal TotalAmount { get; set; } = 0;
    public decimal OutstandingAmount { get; set; } = 0;

    public string? GeneratedByJobId { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateOnly? DueDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<InvoiceItem> Items { get; set; } = [];
    public ICollection<PaymentAllocation> PaymentAllocations { get; set; } = [];
}
