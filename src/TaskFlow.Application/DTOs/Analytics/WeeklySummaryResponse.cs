namespace TaskFlow.Application.DTOs.Analytics;

public sealed record DailyProductivityItem(
    DateOnly Date,
    string DayOfWeek,
    int TasksCompleted,
    TimeSpentDto TimeTracked);

public sealed record MostProductiveDayDto(
    DateOnly Date,
    string DayOfWeek,
    int TasksCompleted,
    TimeSpentDto TimeTracked);

public sealed record WeeklySummaryResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<DailyProductivityItem> Days,
    MostProductiveDayDto? MostProductiveDay,
    int TotalTasksCompleted,
    double TotalHoursTracked,
    TimeSpentDto TotalTimeTracked);
