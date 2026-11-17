using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>All non-revoked tokens of a user (used to revoke everything on reuse detection / logout).</summary>
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(string userId, DateTime utcNow, CancellationToken ct = default);

    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    void Update(RefreshToken token);
}
