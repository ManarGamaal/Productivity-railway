namespace TaskFlow.Application.DTOs.Analytics;

public sealed record ProjectBreakdownItem(
    Guid? ProjectId,
    string ProjectTitle,
    string? ColorCode,
    int TasksCompleted,
    TimeSpentDto TimeTracked);

/// <summary>
/// CompletionRatePercent = (tasks due that day that are Completed) / (tasks due that day) * 100.
/// </summary>
public sealed record DailySummaryResponse(
    DateOnly Date,
    int TasksCompleted,
    int TasksDue,
    int TasksDueAndCompleted,
    double CompletionRatePercent,
    TimeSpentDto TimeTracked,
    IReadOnlyList<ProjectBreakdownItem> ProjectBreakdown);
