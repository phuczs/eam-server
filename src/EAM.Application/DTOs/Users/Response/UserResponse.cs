public class UserResponse
{
    public Guid Id { get; set; }
    public string Role { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string IdentityLinkStatus { get; set; } = null!;
    public string AccountStatus { get; set; } = null!;

    public string? OfficialId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ResidentialAddress { get; set; }

    public decimal CurrentBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}