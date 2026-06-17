using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EAM.Application.DTOs.Users.Request
{
    public class ReopenUserRequestDto
    {
        [Required(ErrorMessage = "Justification is required when reopening an account.")]
        public string Justification { get; set; } = null!;
    }
}
