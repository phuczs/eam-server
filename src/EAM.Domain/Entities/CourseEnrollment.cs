namespace EAM.Domain.Entities;

public class CourseEnrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string Status { get; set; } = "active";
    public bool ExternalPaymentOnly { get; set; } = false;
    public string EnrollmentSource { get; set; } = "manual";

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public ICollection<InvoiceItem> InvoiceItems { get; set; } = [];
    public ICollection<FasApplication> FasApplications { get; set; } = [];
}
