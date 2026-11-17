using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.API.Middleware;

/// <summary>
/// Converts every unhandled exception into a standard RFC 9457 ProblemDetails response.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected; nothing to send back.
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex, "Exception after the response had started.");
                throw;
            }

            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        ProblemDetails problem;

        switch (exception)
        {
            case RequestValidationException validation:
                problem = new ValidationProblemDetails(
                    validation.Errors.ToDictionary(e => e.Key, e => e.Value))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                };
                break;

            case NotFoundException notFound:
                problem = Build(StatusCodes.Status404NotFound, "Resource not found.", notFound.Message,
                    "https://tools.ietf.org/html/rfc9110#section-15.5.5");
                break;

            case ConflictException conflict:
                problem = Build(StatusCodes.Status409Conflict, "Conflict.", conflict.Message,
                    "https://tools.ietf.org/html/rfc9110#section-15.5.10");
                break;

            case BusinessRuleException rule:
                problem = Build(StatusCodes.Status422UnprocessableEntity, "Business rule violation.", rule.Message,
                    "https://tools.ietf.org/html/rfc9110#section-15.5.21");
                break;

            case AuthenticationFailedException auth:
                problem = Build(StatusCodes.Status401Unauthorized, "Authentication failed.", auth.Message,
                    "https://tools.ietf.org/html/rfc9110#section-15.5.2");
                break;

            default:
                _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                problem = Build(
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred.",
                    _environment.IsDevelopment() ? exception.ToString() : "Please try again later or contact support.",
                    "https://tools.ietf.org/html/rfc9110#section-15.6.1");
                break;
        }

        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problem, problem.GetType(), JsonOptions);
        await context.Response.WriteAsync(json, context.RequestAborted);
    }

    private static ProblemDetails Build(int status, string title, string detail, string type) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Type = type
    };
}
