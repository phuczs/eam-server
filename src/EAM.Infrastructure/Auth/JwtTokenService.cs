using EAM.Application.Interfaces.Services;
using EAM.Application.Options;
using EAM.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EAM.Application.Services
{
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _opts;

        public JwtTokenService(IOptions<JwtOptions> opts) => _opts = opts.Value;

        public (string accessToken, DateTime expiresAt) IssueAccessToken(User user)
        {
            var signingKey = GetSigningKeyBytes();
            var key = new SymmetricSecurityKey(signingKey);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_opts.AccessTokenMinutes);

            var token = new JwtSecurityToken(
                _opts.Issuer,
                _opts.Audience,
                claims: new[]
                {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("role", user.Role),
                },
                expires: expires,
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }

        public (string rawToken, RefreshToken entity) IssueRefreshToken(Guid userId)
        {
            var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var entity = new RefreshToken
            {
                UserId = userId,
                TokenHash = HashToken(raw),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(_opts.RefreshTokenDays),
            };
            return (raw, entity);
        }

        public string HashToken(string rawToken)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        private byte[] GetSigningKeyBytes()
        {
            if (string.IsNullOrWhiteSpace(_opts.SigningKey))
                throw new InvalidOperationException("Jwt:SigningKey is required.");

            var key = Encoding.UTF8.GetBytes(_opts.SigningKey);
            if (key.Length < 32)
                throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes for HMAC-SHA256.");

            return key;
        }
    }

}
