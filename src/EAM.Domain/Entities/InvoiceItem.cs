namespace EAM.Domain.Entities;

public class InvoiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public Guid? EnrollmentId { get; set; }
    public CourseEnrollment? Enrollment { get; set; }

    public string? FeeComponent { get; set; }

    public Guid? FasAwardId { get; set; }
    public FasAward? FasAward { get; set; }

    public string FeeType { get; set; } = null!;
    public string Description { get; set; } = null!;

    public int Quantity { get; set; } = 1;
    public decimal UnitAmount { get; set; }
    public decimal GstAmount { get; set; } = 0;
    public decimal SubsidyAmount { get; set; } = 0;
    public decimal WaiverAmount { get; set; } = 0;
    public decimal LineTotal { get; set; }

    public string? CalculationJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
