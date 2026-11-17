namespace TaskFlow.Application.DTOs.Auth;

public sealed record AuthResponse(
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
