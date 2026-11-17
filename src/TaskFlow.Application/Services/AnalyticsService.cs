using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs.Analytics;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

/// <summary>
/// All calendar days are UTC days. A task counts for the day of its CompletedAtUtc,
/// and a time log counts for the day it STARTED.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
    private static readonly Guid NoProjectKey = Guid.Empty;

    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public AnalyticsService(IUnitOfWork uow, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _uow = uow;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<DailySummaryResponse> GetDailySummaryAsync(DateOnly? date, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var day = date ?? DateOnly.FromDateTime(_clock.UtcNow);
        var from = StartOfDay(day);
        var to = from.AddDays(1);

        var completedTasks = await _uow.Tasks.GetCompletedBetweenAsync(userId, from, to, ct);
        var logs = await _uow.TimeLogs.GetFinishedBetweenAsync(userId, from, to, ct);
        var dueCount = await _uow.Tasks.CountDueBetweenAsync(userId, from, to, ct);
        var dueCompletedCount = await _uow.Tasks.CountDueAndCompletedBetweenAsync(userId, from, to, ct);

        var totalMinutes = logs.Sum(l => l.DurationInMinutes);
        var completionRate = dueCount == 0
            ? 0d
            : Math.Round(dueCompletedCount * 100d / dueCount, 2);

        // ---- breakdown by project ----
        var projectInfo = new Dictionary<Guid, (string Title, string? Color)>();
        foreach (var task in completedTasks)
        {
            if (task.Project is not null)
            {
                projectInfo[task.Project.Id] = (task.Project.Title, task.Project.ColorCode);
            }
        }

        foreach (var log in logs)
        {
            var project = log.TaskItem?.Project;
            if (project is not null)
            {
                projectInfo[project.Id] = (project.Title, project.ColorCode);
            }
        }

        var completedByProject = completedTasks
            .GroupBy(t => t.ProjectId ?? NoProjectKey)
            .ToDictionary(g => g.Key, g => g.Count());

        var minutesByProject = logs
            .GroupBy(l => l.TaskItem?.ProjectId ?? NoProjectKey)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.DurationInMinutes));

        var breakdown = completedByProject.Keys
            .Union(minutesByProject.Keys)
            .Select(key =>
            {
                var tasksCompleted = completedByProject.TryGetValue(key, out var count) ? count : 0;
                var minutes = minutesByProject.TryGetValue(key, out var m) ? m : 0d;

                if (key == NoProjectKey)
                {
                    return new ProjectBreakdownItem(null, "No project", null, tasksCompleted, TimeSpentDto.FromMinutes(minutes));
                }

                var title = "Unknown project";
                string? color = null;
                if (projectInfo.TryGetValue(key, out var info))
                {
                    title = info.Title;
                    color = info.Color;
                }

                return new ProjectBreakdownItem(key, title, color, tasksCompleted, TimeSpentDto.FromMinutes(minutes));
            })
            .OrderByDescending(item => item.TimeTracked.TotalMinutes)
            .ThenByDescending(item => item.TasksCompleted)
            .ToList();

        return new DailySummaryResponse(
            day,
            completedTasks.Count,
            dueCount,
            dueCompletedCount,
            completionRate,
            TimeSpentDto.FromMinutes(totalMinutes),
            breakdown);
    }

    public async Task<WeeklySummaryResponse> GetWeeklySummaryAsync(DateOnly? startDate, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var start = startDate ?? StartOfCurrentWeek(DateOnly.FromDateTime(_clock.UtcNow));
        var from = StartOfDay(start);
        var to = from.AddDays(7);

        var completedTasks = await _uow.Tasks.GetCompletedBetweenAsync(userId, from, to, ct);
        var logs = await _uow.TimeLogs.GetFinishedBetweenAsync(userId, from, to, ct);

        var completedPerDay = completedTasks
            .GroupBy(t => DateOnly.FromDateTime(t.CompletedAtUtc!.Value))
            .ToDictionary(g => g.Key, g => g.Count());

        var minutesPerDay = logs
            .GroupBy(l => DateOnly.FromDateTime(l.StartTime))
            .ToDictionary(g => g.Key, g => g.Sum(l => l.DurationInMinutes));

        var days = new List<(DateOnly Date, int Tasks, double Minutes)>(7);
        for (var i = 0; i < 7; i++)
        {
            var date = start.AddDays(i);
            var tasks = completedPerDay.TryGetValue(date, out var t) ? t : 0;
            var minutes = minutesPerDay.TryGetValue(date, out var m) ? m : 0d;
            days.Add((date, tasks, minutes));
        }

        var dayItems = days
            .Select(d => new DailyProductivityItem(
                d.Date,
                d.Date.DayOfWeek.ToString(),
                d.Tasks,
                TimeSpentDto.FromMinutes(d.Minutes)))
            .ToList();

        MostProductiveDayDto? mostProductive = null;
        var best = days
            .Where(d => d.Tasks > 0 || d.Minutes > 0)
            .OrderByDescending(d => d.Tasks)
            .ThenByDescending(d => d.Minutes)
            .ThenBy(d => d.Date)
            .Cast<(DateOnly Date, int Tasks, double Minutes)?>()
            .FirstOrDefault();

        if (best.HasValue)
        {
            mostProductive = new MostProductiveDayDto(
                best.Value.Date,
                best.Value.Date.DayOfWeek.ToString(),
                best.Value.Tasks,
                TimeSpentDto.FromMinutes(best.Value.Minutes));
        }

        var totalMinutes = days.Sum(d => d.Minutes);
        var totalTasks = days.Sum(d => d.Tasks);

        return new WeeklySummaryResponse(
            start,
            start.AddDays(6),
            dayItems,
            mostProductive,
            totalTasks,
            Math.Round(totalMinutes / 60d, 2),
            TimeSpentDto.FromMinutes(totalMinutes));
    }

    private static DateTime StartOfDay(DateOnly day) => day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    private static DateOnly StartOfCurrentWeek(DateOnly today)
    {
        var diff = ((int)today.DayOfWeek + 6) % 7; // Monday = 0
        return today.AddDays(-diff);
    }
}
