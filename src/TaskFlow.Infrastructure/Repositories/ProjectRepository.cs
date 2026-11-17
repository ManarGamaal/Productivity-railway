using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Project?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default)
        => _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct);

    public async Task<IReadOnlyList<Project>> GetAllByUserAsync(string userId, CancellationToken ct = default)
        => await _context.Projects
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.Title)
            .ToListAsync(ct);

    public Task<bool> ExistsAsync(Guid id, string userId, CancellationToken ct = default)
        => _context.Projects.AnyAsync(p => p.Id == id && p.UserId == userId, ct);

    public async Task AddAsync(Project project, CancellationToken ct = default)
        => await _context.Projects.AddAsync(project, ct);

    public void Update(Project project)
    {
        var entry = _context.Entry(project);
        if (entry.State == EntityState.Detached)
        {
            entry.State = EntityState.Modified;
        }
    }

    public void Remove(Project project) => _context.Projects.Remove(project);
}
