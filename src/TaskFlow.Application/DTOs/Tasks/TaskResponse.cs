using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.DTOs.Tasks;

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    DateTime? DueDate,
    TaskItemStatus Status,
    TaskPriority Priority,
    Guid? ProjectId,
    string? ProjectTitle,
    DateTime? CompletedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
