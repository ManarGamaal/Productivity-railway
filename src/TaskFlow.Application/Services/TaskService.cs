using FluentValidation;
using TaskFlow.Application.Common.Extensions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs.Common;
using TaskFlow.Application.DTOs.Tasks;
using TaskFlow.Application.Interfaces;
using TaskFlow.Application.Mappings;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.Filters;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

public sealed class TaskService : ITaskService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IValidator<CreateTaskRequest> _createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator;
    private readonly IValidator<ChangeTaskStatusRequest> _statusValidator;
    private readonly IValidator<TaskFilterRequest> _filterValidator;

    public TaskService(
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator,
        IValidator<ChangeTaskStatusRequest> statusValidator,
        IValidator<TaskFilterRequest> filterValidator)
    {
        _uow = uow;
        _currentUser = currentUser;
        _clock = clock;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
        _filterValidator = filterValidator;
    }

    public async Task<PagedResponse<TaskResponse>> GetPagedAsync(TaskFilterRequest filter, CancellationToken ct = default)
    {
        await _filterValidator.EnsureValidAsync(filter, ct);

        var domainFilter = new TaskQueryFilter
        {
            UserId = _currentUser.UserId,
            Status = filter.Status,
            Priority = filter.Priority,
            ProjectId = filter.ProjectId,
            DueFromUtc = filter.DueFrom.ToUtc(),
            DueToUtc = filter.DueTo.ToUtc(),
            SortBy = filter.SortBy,
            SortDescending = filter.SortDescending,
            Page = filter.Page,
            PageSize = filter.PageSize
        };

        var result = await _uow.Tasks.GetPagedAsync(domainFilter, ct);
        return result.ToPagedResponse(t => t.ToResponse());
    }

    public async Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var task = await FindOwnedAsync(id, ct);
        return task.ToResponse();
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        await _createValidator.EnsureValidAsync(request, ct);

        var userId = _currentUser.UserId;
        var projectTitle = await ResolveProjectAsync(request.ProjectId, userId, ct);

        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            DueDate = request.DueDate.ToUtc(),
            Priority = request.Priority,
            ProjectId = request.ProjectId,
            UserId = userId
        };
        task.ChangeStatus(request.Status, _clock.UtcNow);

        await _uow.Tasks.AddAsync(task, ct);
        await _uow.SaveChangesAsync(ct);

        var response = task.ToResponse();
        return response with { ProjectTitle = projectTitle };
    }

    public async Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        await _updateValidator.EnsureValidAsync(request, ct);

        var userId = _currentUser.UserId;
        var task = await FindOwnedAsync(id, ct);
        var projectTitle = await ResolveProjectAsync(request.ProjectId, userId, ct);

        task.Title = request.Title.Trim();
        task.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        task.DueDate = request.DueDate.ToUtc();
        task.Priority = request.Priority;
        task.ProjectId = request.ProjectId;

        await ApplyStatusAsync(task, request.Status, ct);

        _uow.Tasks.Update(task);
        await _uow.SaveChangesAsync(ct);

        var response = task.ToResponse();
        return response with { ProjectTitle = projectTitle };
    }

    public async Task<TaskResponse> ChangeStatusAsync(Guid id, ChangeTaskStatusRequest request, CancellationToken ct = default)
    {
        await _statusValidator.EnsureValidAsync(request, ct);

        var task = await FindOwnedAsync(id, ct);
        await ApplyStatusAsync(task, request.Status, ct);

        _uow.Tasks.Update(task);
        await _uow.SaveChangesAsync(ct);

        return task.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await FindOwnedAsync(id, ct);

        // Time logs of the task are removed by the database (ON DELETE CASCADE).
        _uow.Tasks.Remove(task);
        await _uow.SaveChangesAsync(ct);
    }

    private async Task<TaskItem> FindOwnedAsync(Guid id, CancellationToken ct)
    {
        return await _uow.Tasks.GetByIdAsync(id, _currentUser.UserId, ct)
               ?? throw new NotFoundException(nameof(TaskItem), id);
    }

    /// <summary>Verifies the project belongs to the caller and returns its title (null when no project).</summary>
    private async Task<string?> ResolveProjectAsync(Guid? projectId, string userId, CancellationToken ct)
    {
        if (projectId is null)
        {
            return null;
        }

        var project = await _uow.Projects.GetByIdAsync(projectId.Value, userId, ct)
                      ?? throw new NotFoundException(nameof(Project), projectId.Value);
        return project.Title;
    }

    /// <summary>
    /// Changes the status and, when a task is completed, stops a timer that is still running on it.
    /// </summary>
    private async Task ApplyStatusAsync(TaskItem task, TaskItemStatus newStatus, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        task.ChangeStatus(newStatus, now);

        if (newStatus != TaskItemStatus.Completed)
        {
            return;
        }

        var active = await _uow.TimeLogs.GetActiveTimerAsync(task.UserId, ct);
        if (active is not null && active.TaskItemId == task.Id)
        {
            active.Stop(now);
            _uow.TimeLogs.Update(active);
        }
    }
}
