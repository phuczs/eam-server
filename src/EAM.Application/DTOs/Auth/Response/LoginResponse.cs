using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.DTOs.Auth.Response
{
    public record LoginResponse(
     bool MfaRequired,
     Guid UserId,
     string? AccessToken,
     DateTime? AccessTokenExpiresUtc);
}
