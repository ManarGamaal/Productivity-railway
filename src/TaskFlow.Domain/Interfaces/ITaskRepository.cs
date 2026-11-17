using TaskFlow.Domain.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Filters;

namespace TaskFlow.Domain.Interfaces;

public interface ITaskRepository
{
    /// <summary>Returns the task only if it belongs to <paramref name="userId"/>.</summary>
    Task<TaskItem?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default);

    Task<PagedResult<TaskItem>> GetPagedAsync(TaskQueryFilter filter, CancellationToken ct = default);

    /// <summary>Tasks completed in [fromUtc, toUtcExclusive), with Project loaded.</summary>
    Task<IReadOnlyList<TaskItem>> GetCompletedBetweenAsync(
        string userId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken ct = default);

    /// <summary>Number of tasks whose DueDate is in [fromUtc, toUtcExclusive).</summary>
    Task<int> CountDueBetweenAsync(
        string userId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken ct = default);

    /// <summary>Number of tasks due in [fromUtc, toUtcExclusive) that are Completed.</summary>
    Task<int> CountDueAndCompletedBetweenAsync(
        string userId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken ct = default);

    Task AddAsync(TaskItem task, CancellationToken ct = default);
    void Update(TaskItem task);
    void Remove(TaskItem task);
}
