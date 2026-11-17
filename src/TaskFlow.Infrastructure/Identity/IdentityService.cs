using Microsoft.AspNetCore.Identity;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Models;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserIdentity> CreateUserAsync(
        string email, string password, string firstName, string lastName, CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new RequestValidationException(MapIdentityErrors(result.Errors));
        }

        return await ToIdentityAsync(user);
    }

    public async Task<UserIdentity?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new AuthenticationFailedException("The account is temporarily locked because of too many failed attempts. Try again later.");
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            await _userManager.AccessFailedAsync(user);
            return null;
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return await ToIdentityAsync(user);
    }

    public async Task<UserIdentity?> GetByIdAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? null : await ToIdentityAsync(user);
    }

    private async Task<UserIdentity> ToIdentityAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserIdentity(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            roles.ToList());
    }

    private static Dictionary<string, string[]> MapIdentityErrors(IEnumerable<IdentityError> errors)
    {
        return errors
            .GroupBy(e => KeyFor(e.Code), e => e.Description)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    private static string KeyFor(string code)
    {
        if (code.StartsWith("Password", StringComparison.OrdinalIgnoreCase))
        {
            return "Password";
        }

        if (code.Contains("Email", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("UserName", StringComparison.OrdinalIgnoreCase))
        {
            return "Email";
        }

        return "Identity";
    }
}
