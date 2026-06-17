using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.DTOs.Users.Request;
public class UserDetailResponse
{
    public Guid Id { get; set; }

    // 1. Account Holder Details
    public string? OfficialId { get; set; } //Mask only show 4 last
    public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }

    // 2. Account Status
    public string Status { get; set; } = null!;
    public string AccountStatus { get; set; } = null!;

    // 3. Account Balance
    public decimal CurrentBalance { get; set; }

    // 4. Registered Bank Account
    public string? BankAccountNumber { get; set; } // Masked Account Number
    public string? BankName { get; set; }

    public DateTime CreatedAt { get; set; }
}
