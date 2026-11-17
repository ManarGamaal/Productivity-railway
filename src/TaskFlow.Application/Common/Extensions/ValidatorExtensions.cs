using FluentValidation;
using TaskFlow.Application.Common.Exceptions;

namespace TaskFlow.Application.Common.Extensions;

public static class ValidatorExtensions
{
    /// <summary>Validates and throws <see cref="RequestValidationException"/> on failure.</summary>
    public static async Task EnsureValidAsync<T>(
        this IValidator<T> validator, T instance, CancellationToken ct = default)
    {
        var result = await validator.ValidateAsync(instance, ct);
        if (!result.IsValid)
        {
            throw new RequestValidationException(result.Errors);
        }
    }
}
