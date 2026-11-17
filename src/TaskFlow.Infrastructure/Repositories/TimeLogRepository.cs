using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Repositories;

public sealed class TimeLogRepository : ITimeLogRepository
{
    private readonly AppDbContext _context;

    public TimeLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<TimeLog?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default)
        => _context.TimeLogs
            .Include(l => l.TaskItem)
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId, ct);

    public Task<TimeLog?> GetActiveTimerAsync(string userId, CancellationToken ct = default)
        => _context.TimeLogs
            .Include(l => l.TaskItem)
            .FirstOrDefaultAsync(l => l.UserId == userId && l.EndTime == null, ct);

    public async Task<IReadOnlyList<TimeLog>> GetByTaskAsync(Guid taskItemId, string userId, CancellationToken ct = default)
        => await _context.TimeLogs
            .AsNoTracking()
            .Include(l => l.TaskItem)
            .Where(l => l.TaskItemId == taskItemId && l.UserId == userId)
            .OrderByDescending(l => l.StartTime)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TimeLog>> GetFinishedBetweenAsync(
        string userId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken ct = default)
        => await _context.TimeLogs
            .AsNoTracking()
            .Include(l => l.TaskItem)
                .ThenInclude(t => t!.Project)
            .Where(l => l.UserId == userId
                        && l.EndTime != null
                        && l.StartTime >= fromUtc
                        && l.StartTime < toUtcExclusive)
            .ToListAsync(ct);

    public Task<bool> HasOverlapAsync(
        string userId, DateTime startUtc, DateTime endUtc, Guid? excludeLogId = null, CancellationToken ct = default)
        => _context.TimeLogs
            .Where(l => l.UserId == userId && (excludeLogId == null || l.Id != excludeLogId))
            .AnyAsync(l => l.StartTime < endUtc && (l.EndTime == null || l.EndTime > startUtc), ct);

    public async Task AddAsync(TimeLog timeLog, CancellationToken ct = default)
        => await _context.TimeLogs.AddAsync(timeLog, ct);

    public void Update(TimeLog timeLog)
    {
        var entry = _context.Entry(timeLog);
        if (entry.State == EntityState.Detached)
        {
            entry.State = EntityState.Modified;
        }
    }

    public void Remove(TimeLog timeLog) => _context.TimeLogs.Remove(timeLog);
}
