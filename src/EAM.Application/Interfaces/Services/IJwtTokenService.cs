using EAM.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EAM.Application.Interfaces.Services
{
    public interface IJwtTokenService
    {
        (string accessToken, DateTime expiresAt) IssueAccessToken(User user);

        (string rawToken, RefreshToken entity) IssueRefreshToken(Guid userId);

        string HashToken(string rawToken);
    }

}
