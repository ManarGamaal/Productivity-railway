using System.Security.Claims;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.API.Services;

/// <summary>Reads the authenticated caller from the JWT claims of the current request.</summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string UserId
    {
        get
        {
            var principal = _accessor.HttpContext?.User;
            var id = principal?.FindFirst(AppClaimTypes.UserId)?.Value
                     ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return string.IsNullOrWhiteSpace(id)
                ? throw new AuthenticationFailedException("The request is not authenticated.")
                : id;
        }
    }

    public string? Email =>
        _accessor.HttpContext?.User.FindFirst(AppClaimTypes.Email)?.Value
        ?? _accessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;
}
