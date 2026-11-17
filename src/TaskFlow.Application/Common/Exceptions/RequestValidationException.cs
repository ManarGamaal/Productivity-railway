using FluentValidation.Results;

namespace TaskFlow.Application.Common.Exceptions;

/// <summary>Thrown when a request fails validation. Maps to HTTP 400.</summary>
public sealed class RequestValidationException : Exception
{
    public RequestValidationException() : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public RequestValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(f => string.IsNullOrWhiteSpace(f.PropertyName) ? "request" : f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.Distinct().ToArray());
    }

    public RequestValidationException(IDictionary<string, string[]> errors) : this()
    {
        Errors = new Dictionary<string, string[]>(errors);
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
