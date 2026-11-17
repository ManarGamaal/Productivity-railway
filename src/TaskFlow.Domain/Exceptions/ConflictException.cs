namespace TaskFlow.Domain.Exceptions;

/// <summary>Maps to HTTP 409 (e.g. duplicate email, resource state conflict).</summary>
public sealed class ConflictException : DomainException
{
    public ConflictException(string message) : base(message)
    {
    }
}
