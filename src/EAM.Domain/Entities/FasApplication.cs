namespace EAM.Domain.Entities;

public class FasApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SchemeId { get; set; }
    public FasScheme Scheme { get; set; } = null!;

    public Guid? EnrollmentId { get; set; }
    public CourseEnrollment? Enrollment { get; set; }

    public string Status { get; set; } = "draft";
    public int? HouseholdSize { get; set; }
    public decimal? HouseholdIncome { get; set; }
    public decimal? PerCapitaIncome { get; set; }
    public string? EvaluationResultJson { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public Guid? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }

    public DateTime? ReviewedAt { get; set; }
    public string? ReviewRemarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<FasApplicationEvidence> Evidences { get; set; } = [];
    public FasAward? Award { get; set; }
}
