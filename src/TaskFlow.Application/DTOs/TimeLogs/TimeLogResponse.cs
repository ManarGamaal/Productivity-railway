namespace TaskFlow.Application.DTOs.TimeLogs;

public sealed record TimeLogResponse(
    Guid Id,
    Guid TaskId,
    string? TaskTitle,
    DateTime StartTime,
    DateTime? EndTime,
    double DurationInMinutes,
    bool IsRunning,
    string? Notes,
    DateTime CreatedAtUtc);
