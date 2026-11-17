namespace TaskFlow.Application.Common.Interfaces;

/// <summary>Exposes the authenticated caller. Implemented in the API layer from the JWT claims.</summary>
public interface ICurrentUserService
{
    /// <summary>The caller's user id. Throws AuthenticationFailedException when not authenticated.</summary>
    string UserId { get; }

    string? Email { get; }
}
