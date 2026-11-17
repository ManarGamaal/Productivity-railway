using FluentValidation;
using TaskFlow.Application.Common.Extensions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Models;
using TaskFlow.Application.DTOs.Auth;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IIdentityService _identity;
    private readonly ITokenService _tokens;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshValidator;

    public AuthService(
        IIdentityService identity,
        ITokenService tokens,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        ICurrentUserService currentUser,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshValidator)
    {
        _identity = identity;
        _tokens = tokens;
        _uow = uow;
        _clock = clock;
        _currentUser = currentUser;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken ct = default)
    {
        await _registerValidator.EnsureValidAsync(request, ct);

        var user = await _identity.CreateUserAsync(
            request.Email.Trim(),
            request.Password,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            ct);

        return await IssueTokensAsync(user, ipAddress, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        await _loginValidator.EnsureValidAsync(request, ct);

        var user = await _identity.ValidateCredentialsAsync(request.Email.Trim(), request.Password, ct)
                   ?? throw new AuthenticationFailedException("Invalid email or password.");

        return await IssueTokensAsync(user, ipAddress, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken ct = default)
    {
        await _refreshValidator.EnsureValidAsync(request, ct);

        var now = _clock.UtcNow;
        var hash = _tokens.HashToken(request.RefreshToken);
        var stored = await _uow.RefreshTokens.GetByHashAsync(hash, ct)
                     ?? throw new AuthenticationFailedException("Invalid refresh token.");

        if (stored.RevokedAtUtc is not null)
        {
            // A revoked token is being replayed: assume theft and kill every session of this user.
            var activeTokens = await _uow.RefreshTokens.GetActiveByUserAsync(stored.UserId, now, ct);
            foreach (var token in activeTokens)
            {
                token.Revoke(now);
                _uow.RefreshTokens.Update(token);
            }

            await _uow.SaveChangesAsync(ct);
            throw new AuthenticationFailedException("Refresh token has been revoked. Please sign in again.");
        }

        if (stored.IsExpired(now))
        {
            throw new AuthenticationFailedException("Refresh token has expired. Please sign in again.");
        }

        var user = await _identity.GetByIdAsync(stored.UserId, ct)
                   ?? throw new AuthenticationFailedException("User no longer exists.");

        // Rotation: the old token is revoked and replaced by a brand-new one in a single save.
        var access = _tokens.GenerateAccessToken(user);
        var (rawRefresh, newRefresh) = CreateRefreshToken(user.Id, ipAddress, now);

        stored.Revoke(now, newRefresh.TokenHash);
        _uow.RefreshTokens.Update(stored);
        await _uow.RefreshTokens.AddAsync(newRefresh, ct);
        await _uow.SaveChangesAsync(ct);

        return BuildResponse(user, access, rawRefresh, newRefresh.ExpiresAtUtc);
    }

    public async Task RevokeAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        await _refreshValidator.EnsureValidAsync(request, ct);

        var userId = _currentUser.UserId;
        var hash = _tokens.HashToken(request.RefreshToken);
        var stored = await _uow.RefreshTokens.GetByHashAsync(hash, ct);

        if (stored is null || stored.UserId != userId)
        {
            throw new AuthenticationFailedException("Invalid refresh token.");
        }

        stored.Revoke(_clock.UtcNow);
        _uow.RefreshTokens.Update(stored);
        await _uow.SaveChangesAsync(ct);
    }

    private async Task<AuthResponse> IssueTokensAsync(UserIdentity user, string? ipAddress, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var access = _tokens.GenerateAccessToken(user);
        var (rawRefresh, refreshEntity) = CreateRefreshToken(user.Id, ipAddress, now);

        await _uow.RefreshTokens.AddAsync(refreshEntity, ct);
        await _uow.SaveChangesAsync(ct);

        return BuildResponse(user, access, rawRefresh, refreshEntity.ExpiresAtUtc);
    }

    private (string Raw, RefreshToken Entity) CreateRefreshToken(string userId, string? ipAddress, DateTime now)
    {
        var raw = _tokens.GenerateRefreshToken();
        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = _tokens.HashToken(raw),
            ExpiresAtUtc = now.Add(_tokens.RefreshTokenLifetime),
            CreatedByIp = ipAddress
        };

        return (raw, entity);
    }

    private static AuthResponse BuildResponse(UserIdentity user, AccessToken access, string rawRefresh, DateTime refreshExpiresAtUtc)
        => new(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            access.Token,
            access.ExpiresAtUtc,
            rawRefresh,
            refreshExpiresAtUtc);
}
