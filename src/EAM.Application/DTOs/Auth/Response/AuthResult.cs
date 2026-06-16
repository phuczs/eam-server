namespace EAM.Application.DTOs.Auth.Response;

/// <summary>Placeholder authentication result DTO.</summary>
public record AuthResult(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
