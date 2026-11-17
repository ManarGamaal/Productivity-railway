namespace TaskFlow.Application.DTOs.Projects;

public sealed record ProjectResponse(
    Guid Id,
    string Title,
    string? Description,
    string ColorCode,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
