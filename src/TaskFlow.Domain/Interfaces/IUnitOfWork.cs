namespace TaskFlow.Domain.Interfaces;

/// <summary>
/// Groups repositories and commits all changes in a single transaction.
/// </summary>
public interface IUnitOfWork
{
    IProjectRepository Projects { get; }
    ITaskRepository Tasks { get; }
    ITimeLogRepository TimeLogs { get; }
    IRefreshTokenRepository RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
