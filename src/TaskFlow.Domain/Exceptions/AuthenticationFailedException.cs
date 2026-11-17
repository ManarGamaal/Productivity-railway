namespace TaskFlow.Domain.Exceptions;

/// <summary>Maps to HTTP 401 (bad credentials, invalid/expired/revoked refresh token).</summary>
public sealed class AuthenticationFailedException : DomainException
{
    public AuthenticationFailedException(string message) : base(message)
    {
    }
}
