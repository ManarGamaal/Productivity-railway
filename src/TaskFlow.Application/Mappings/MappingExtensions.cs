using TaskFlow.Application.DTOs.Common;
using TaskFlow.Application.DTOs.Projects;
using TaskFlow.Application.DTOs.Tasks;
using TaskFlow.Application.DTOs.TimeLogs;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Mappings;

/// <summary>Manual, explicit mapping between entities and DTOs (no reflection, no surprises).</summary>
public static class MappingExtensions
{
    public const string DefaultProjectColor = "#3B82F6";

    public static ProjectResponse ToResponse(this Project project) => new(
        project.Id,
        project.Title,
        project.Description,
        project.ColorCode,
        project.CreatedAtUtc,
        project.UpdatedAtUtc);

    public static TaskResponse ToResponse(this TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.DueDate,
        task.Status,
        task.Priority,
        task.ProjectId,
        task.Project?.Title,
        task.CompletedAtUtc,
        task.CreatedAtUtc,
        task.UpdatedAtUtc);

    public static TimeLogResponse ToResponse(this TimeLog log) => new(
        log.Id,
        log.TaskItemId,
        log.TaskItem?.Title,
        log.StartTime,
        log.EndTime,
        log.DurationInMinutes,
        log.IsRunning,
        log.Notes,
        log.CreatedAtUtc);

    public static PagedResponse<TOut> ToPagedResponse<TIn, TOut>(
        this PagedResult<TIn> result, Func<TIn, TOut> map) => new(
        result.Items.Select(map).ToList(),
        result.TotalCount,
        result.Page,
        result.PageSize,
        result.TotalPages,
        result.HasPreviousPage,
        result.HasNextPage);
}
