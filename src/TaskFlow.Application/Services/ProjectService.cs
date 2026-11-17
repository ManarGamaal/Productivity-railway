using FluentValidation;
using TaskFlow.Application.Common.Extensions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs.Projects;
using TaskFlow.Application.Interfaces;
using TaskFlow.Application.Mappings;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

public sealed class ProjectService : IProjectService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<CreateProjectRequest> _createValidator;
    private readonly IValidator<UpdateProjectRequest> _updateValidator;

    public ProjectService(
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IValidator<CreateProjectRequest> createValidator,
        IValidator<UpdateProjectRequest> updateValidator)
    {
        _uow = uow;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<ProjectResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var projects = await _uow.Projects.GetAllByUserAsync(_currentUser.UserId, ct);
        return projects.Select(p => p.ToResponse()).ToList();
    }

    public async Task<ProjectResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var project = await FindOwnedAsync(id, ct);
        return project.ToResponse();
    }

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        await _createValidator.EnsureValidAsync(request, ct);

        var project = new Project
        {
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            ColorCode = string.IsNullOrWhiteSpace(request.ColorCode) ? MappingExtensions.DefaultProjectColor : request.ColorCode.Trim(),
            UserId = _currentUser.UserId
        };

        await _uow.Projects.AddAsync(project, ct);
        await _uow.SaveChangesAsync(ct);

        return project.ToResponse();
    }

    public async Task<ProjectResponse> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken ct = default)
    {
        await _updateValidator.EnsureValidAsync(request, ct);

        var project = await FindOwnedAsync(id, ct);
        project.Title = request.Title.Trim();
        project.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        project.ColorCode = string.IsNullOrWhiteSpace(request.ColorCode) ? MappingExtensions.DefaultProjectColor : request.ColorCode.Trim();

        _uow.Projects.Update(project);
        await _uow.SaveChangesAsync(ct);

        return project.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var project = await FindOwnedAsync(id, ct);

        // Tasks of the project are kept; the database sets their ProjectId to NULL (ON DELETE SET NULL).
        _uow.Projects.Remove(project);
        await _uow.SaveChangesAsync(ct);
    }

    private async Task<Project> FindOwnedAsync(Guid id, CancellationToken ct)
    {
        return await _uow.Projects.GetByIdAsync(id, _currentUser.UserId, ct)
               ?? throw new NotFoundException(nameof(Project), id);
    }
}
