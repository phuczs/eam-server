using EAM.Application.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.DTOs.Users.Request
{
    public class UserSearchRequest : PageRequest
    {
        public string? Keyword { get; set; }
        public string? Status { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
    }
}
