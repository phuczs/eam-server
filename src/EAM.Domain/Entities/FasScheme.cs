namespace EAM.Domain.Entities;

public class FasScheme
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = "draft";

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public DateOnly? ApplicationStartDate { get; set; }
    public DateOnly? ApplicationEndDate { get; set; }

    public string? ApplicableCoursesJson { get; set; }
    public string? CriteriaJson { get; set; }
    public string? RequiredDocumentsJson { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<FasApplication> Applications { get; set; } = [];
}
