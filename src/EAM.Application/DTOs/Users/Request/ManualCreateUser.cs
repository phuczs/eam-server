namespace EAM.Application.DTOs.Users;

using System.ComponentModel.DataAnnotations;

public class ManualCreateUserRequestDto
{
    [Required]
    public string OfficialId { get; set; } = null!; // NRIC

    [Required]
    public string ExceptionType { get; set; } = null!; // string (Under Age","Over Age")

    [Required]
    public string OverrideJustification { get; set; } = null!; 

    [Required]
    public DateTime AccountStartDate { get; set; }
}