namespace EAM.Domain.Entities;

public class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string CourseCode { get; set; } = null!;
    public string CourseName { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = "draft";

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CourseEnrollment> Enrollments { get; set; } = [];
}
