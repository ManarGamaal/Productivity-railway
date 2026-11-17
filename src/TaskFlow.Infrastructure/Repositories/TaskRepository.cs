using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Filters;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default)
        => _context.TaskItems
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

    public async Task<PagedResult<TaskItem>> GetPagedAsync(TaskQueryFilter filter, CancellationToken ct = default)
    {
        var query = _context.TaskItems
            .AsNoTracking()
            .Include(t => t.Project)
            .Where(t => t.UserId == filter.UserId);

        if (filter.Status.HasValue)
        {
            query = query.Where(t => t.Status == filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == filter.Priority.Value);
        }

        if (filter.ProjectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == filter.ProjectId.Value);
        }

        if (filter.DueFromUtc.HasValue)
        {
            var from = filter.DueFromUtc.Value;
            query = query.Where(t => t.DueDate != null && t.DueDate >= from);
        }

        if (filter.DueToUtc.HasValue)
        {
            var to = filter.DueToUtc.Value;
            query = query.Where(t => t.DueDate != null && t.DueDate <= to);
        }

        var totalCount = await query.CountAsync(ct);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

        var items = await ApplySorting(query, filter.SortBy, filter.SortDescending)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TaskItem>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<TaskItem>> GetCompletedBetweenAsync(
        string userId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken ct = default)
        => await _context.TaskItems
            .AsNoTracking()
            .Include(t => t.Project)
            .Where(t => t.UserId == userId
                        && t.Status == TaskItemStatus.Completed
                        && t.CompletedAtUtc != null
                        && t.CompletedAtUtc >= fromUtc
                        && t.CompletedAtUtc < toUtcExclusive)
            .ToListAsync(ct);

    public Task<int> CountDueBetweenAsync(
        string userId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken ct = default)
        => _context.TaskItems
            .Where(t => t.UserId == userId
                        && t.DueDate != null
                        && t.DueDate >= fromUtc
                        && t.DueDate < toUtcExclusive)
            .CountAsync(ct);

    public Task<int> CountDueAndCompletedBetweenAsync(
        string userId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken ct = default)
        => _context.TaskItems
            .Where(t => t.UserId == userId
                        && t.Status == TaskItemStatus.Completed
                        && t.DueDate != null
                        && t.DueDate >= fromUtc
                        && t.DueDate < toUtcExclusive)
            .CountAsync(ct);

    public async Task AddAsync(TaskItem task, CancellationToken ct = default)
        => await _context.TaskItems.AddAsync(task, ct);

    public void Update(TaskItem task)
    {
        var entry = _context.Entry(task);
        if (entry.State == EntityState.Detached)
        {
            entry.State = EntityState.Modified;
        }
    }

    public void Remove(TaskItem task) => _context.TaskItems.Remove(task);

    private static IOrderedQueryable<TaskItem> ApplySorting(
        IQueryable<TaskItem> query, TaskSortField sortBy, bool descending)
    {
        IOrderedQueryable<TaskItem> ordered = sortBy switch
        {
            // Tasks without a due date always go last.
            TaskSortField.DueDate => descending
                ? query.OrderBy(t => t.DueDate == null).ThenByDescending(t => t.DueDate)
                : query.OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate),
            TaskSortField.Priority => descending
                ? query.OrderByDescending(t => t.Priority)
                : query.OrderBy(t => t.Priority),
            TaskSortField.Status => descending
                ? query.OrderByDescending(t => t.Status)
                : query.OrderBy(t => t.Status),
            TaskSortField.Title => descending
                ? query.OrderByDescending(t => t.Title)
                : query.OrderBy(t => t.Title),
            _ => descending
                ? query.OrderByDescending(t => t.CreatedAtUtc)
                : query.OrderBy(t => t.CreatedAtUtc)
        };

        // Stable tie-breaker so pagination never skips or repeats rows.
        return ordered.ThenBy(t => t.Id);
    }
}
