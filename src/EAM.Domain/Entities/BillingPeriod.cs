namespace EAM.Domain.Entities;

public class BillingPeriod
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int Year { get; set; }
    public int Month { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsClosed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Invoice> Invoices { get; set; } = [];
}
