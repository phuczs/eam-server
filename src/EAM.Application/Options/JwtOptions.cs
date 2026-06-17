using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.Options
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = "eam";
        public string Audience { get; set; } = "eam-clients";
        public int AccessTokenMinutes { get; set; } = 15;
        public int RefreshTokenDays { get; set; } = 7;
        public string SigningKey { get; set; } = string.Empty;
    }
}
