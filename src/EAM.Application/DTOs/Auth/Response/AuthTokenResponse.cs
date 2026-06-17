namespace EAM.Application.DTOs.Auth.Response;

public record AuthTokenResponse(string AccessToken, DateTime ExpiresAtUtc);
