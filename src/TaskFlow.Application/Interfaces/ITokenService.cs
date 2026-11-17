using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Interfaces;

public interface ITokenService
{
    AccessToken GenerateAccessToken(UserIdentity user);

    /// <summary>Generates a cryptographically random raw refresh token.</summary>
    string GenerateRefreshToken();

    /// <summary>One-way hash used to store/look up refresh tokens.</summary>
    string HashToken(string token);

    TimeSpan RefreshTokenLifetime { get; }
}
