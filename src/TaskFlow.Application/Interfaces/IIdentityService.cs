using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Interfaces;

/// <summary>Abstraction over ASP.NET Core Identity (implemented in Infrastructure).</summary>
public interface IIdentityService
{
    /// <summary>
    /// Creates a user. Throws ConflictException for a duplicate email and
    /// RequestValidationException when Identity rejects the password/email.
    /// </summary>
    Task<UserIdentity> CreateUserAsync(
        string email, string password, string firstName, string lastName, CancellationToken ct = default);

    /// <summary>Returns the user when credentials are valid, otherwise null. Handles lockout.</summary>
    Task<UserIdentity?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);

    Task<UserIdentity?> GetByIdAsync(string userId, CancellationToken ct = default);
}
