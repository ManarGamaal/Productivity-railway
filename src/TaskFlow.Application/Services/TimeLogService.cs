using FluentValidation;
using TaskFlow.Application.Common.Extensions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.DTOs.TimeLogs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Application.Mappings;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

public sealed class TimeLogService : ITimeLogService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IValidator<StartTimerRequest> _startValidator;
    private readonly IValidator<StopTimerRequest> _stopValidator;
    private readonly IValidator<ManualTimeLogRequest> _manualValidator;

    public TimeLogService(
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IValidator<StartTimerRequest> startValidator,
        IValidator<StopTimerRequest> stopValidator,
        IValidator<ManualTimeLogRequest> manualValidator)
    {
        _uow = uow;
        _currentUser = currentUser;
        _clock = clock;
        _startValidator = startValidator;
        _stopValidator = stopValidator;
        _manualValidator = manualValidator;
    }

    public async Task<TimeLogResponse> StartTimerAsync(StartTimerRequest request, CancellationToken ct = default)
    {
        await _startValidator.EnsureValidAsync(request, ct);

        var userId = _currentUser.UserId;
        var task = await _uow.Tasks.GetByIdAsync(request.TaskId, userId, ct)
                   ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

        if (task.Status == TaskItemStatus.Completed)
        {
            throw new BusinessRuleException("Cannot start a timer on a completed task. Re-open the task first.");
        }

        var active = await _uow.TimeLogs.GetActiveTimerAsync(userId, ct);
        if (active is not null)
        {
            throw new BusinessRuleException(
                $"You already have a running timer (time log '{active.Id}'). Stop it before starting a new one.");
        }

        var now = _clock.UtcNow;
        var log = TimeLog.StartNew(task.Id, userId, now, NormalizeNotes(request.Notes));

        if (task.Status == TaskItemStatus.Todo)
        {
            task.ChangeStatus(TaskItemStatus.InProgress, now);
            _uow.Tasks.Update(task);
        }

        await _uow.TimeLogs.AddAsync(log, ct);
        await _uow.SaveChangesAsync(ct);

        var response = log.ToResponse();
        return response with { TaskTitle = task.Title };
    }

    public async Task<TimeLogResponse> StopTimerAsync(StopTimerRequest? request, CancellationToken ct = default)
    {
        request ??= new StopTimerRequest();
        await _stopValidator.EnsureValidAsync(request, ct);

        var active = await _uow.TimeLogs.GetActiveTimerAsync(_currentUser.UserId, ct)
                     ?? throw new BusinessRuleException("You do not have a running timer.");

        active.Stop(_clock.UtcNow, NormalizeNotes(request.Notes));
        _uow.TimeLogs.Update(active);
        await _uow.SaveChangesAsync(ct);

        return active.ToResponse();
    }

    public async Task<TimeLogResponse?> GetActiveTimerAsync(CancellationToken ct = default)
    {
        var active = await _uow.TimeLogs.GetActiveTimerAsync(_currentUser.UserId, ct);
        return active?.ToResponse();
    }

    public async Task<TimeLogResponse> CreateManualAsync(ManualTimeLogRequest request, CancellationToken ct = default)
    {
        await _manualValidator.EnsureValidAsync(request, ct);

        var userId = _currentUser.UserId;
        var task = await _uow.Tasks.GetByIdAsync(request.TaskId, userId, ct)
                   ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

        var start = request.StartTime.ToUtc();
        var end = request.EndTime.HasValue
            ? request.EndTime.Value.ToUtc()
            : start.AddMinutes(request.DurationInMinutes!.Value);

        if (end > _clock.UtcNow.AddMinutes(1))
        {
            throw new BusinessRuleException("A time log cannot end in the future.");
        }

        if (await _uow.TimeLogs.HasOverlapAsync(userId, start, end, null, ct))
        {
            throw new BusinessRuleException("This time range overlaps with another time log or your running timer.");
        }

        var log = TimeLog.CreateManual(task.Id, userId, start, end, NormalizeNotes(request.Notes));

        await _uow.TimeLogs.AddAsync(log, ct);
        await _uow.SaveChangesAsync(ct);

        var response = log.ToResponse();
        return response with { TaskTitle = task.Title };
    }

    public async Task<TimeLogResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var log = await _uow.TimeLogs.GetByIdAsync(id, _currentUser.UserId, ct)
                  ?? throw new NotFoundException(nameof(TimeLog), id);
        return log.ToResponse();
    }

    public async Task<IReadOnlyList<TimeLogResponse>> GetByTaskAsync(Guid taskId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var task = await _uow.Tasks.GetByIdAsync(taskId, userId, ct)
                   ?? throw new NotFoundException(nameof(TaskItem), taskId);

        var logs = await _uow.TimeLogs.GetByTaskAsync(task.Id, userId, ct);
        return logs.Select(l => l.ToResponse()).ToList();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var log = await _uow.TimeLogs.GetByIdAsync(id, _currentUser.UserId, ct)
                  ?? throw new NotFoundException(nameof(TimeLog), id);

        _uow.TimeLogs.Remove(log);
        await _uow.SaveChangesAsync(ct);
    }

    private static string? NormalizeNotes(string? notes)
        => string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}
