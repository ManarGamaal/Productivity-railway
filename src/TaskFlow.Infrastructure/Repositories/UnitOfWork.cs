using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    // PostgreSQL SQLSTATE for unique_violation.
    private const string UniqueViolation = "23505";

    private readonly AppDbContext _context;

    public UnitOfWork(
        AppDbContext context,
        IProjectRepository projects,
        ITaskRepository tasks,
        ITimeLogRepository timeLogs,
        IRefreshTokenRepository refreshTokens)
    {
        _context = context;
        Projects = projects;
        Tasks = tasks;
        TimeLogs = timeLogs;
        RefreshTokens = refreshTokens;
    }

    public IProjectRepository Projects { get; }
    public ITaskRepository Tasks { get; }
    public ITimeLogRepository TimeLogs { get; }
    public IRefreshTokenRepository RefreshTokens { get; }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
                                           && pg.SqlState == UniqueViolation)
        {
            // Most common cause: two simultaneous "start timer" requests (unique filtered index).
            throw new ConflictException(
                "The operation conflicts with existing data (for example, a timer is already running).");
        }
    }
}
