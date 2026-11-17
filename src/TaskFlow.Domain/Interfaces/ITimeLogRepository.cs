using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Interfaces;

public interface ITimeLogRepository
{
    Task<TimeLog?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default);

    /// <summary>The user's currently running timer (EndTime == null), or null if none.</summary>
    Task<TimeLog?> GetActiveTimerAsync(string userId, CancellationToken ct = default);

    Task<IReadOnlyList<TimeLog>> GetByTaskAsync(Guid taskItemId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Finished logs whose StartTime is in [fromUtc, toUtcExclusive),
    /// with TaskItem and TaskItem.Project loaded (used by analytics).
    /// </summary>
    Task<IReadOnlyList<TimeLog>> GetFinishedBetweenAsync(
        string userId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken ct = default);

    /// <summary>
    /// True if [startUtc, endUtc) overlaps any other log of the user
    /// (a running timer counts as open-ended). <paramref name="excludeLogId"/> skips one log (for updates).
    /// </summary>
    Task<bool> HasOverlapAsync(
        string userId, DateTime startUtc, DateTime endUtc, Guid? excludeLogId = null, CancellationToken ct = default);

    Task AddAsync(TimeLog timeLog, CancellationToken ct = default);
    void Update(TimeLog timeLog);
    void Remove(TimeLog timeLog);
}
