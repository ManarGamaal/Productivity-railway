using TaskFlow.Application.DTOs.Analytics;

namespace TaskFlow.Application.Interfaces;

public interface IAnalyticsService
{
    /// <summary>Daily summary. When <paramref name="date"/> is null, today (UTC) is used.</summary>
    Task<DailySummaryResponse> GetDailySummaryAsync(DateOnly? date, CancellationToken ct = default);

    /// <summary>Weekly summary starting at <paramref name="startDate"/> (null = Monday of the current UTC week).</summary>
    Task<WeeklySummaryResponse> GetWeeklySummaryAsync(DateOnly? startDate, CancellationToken ct = default);
}
