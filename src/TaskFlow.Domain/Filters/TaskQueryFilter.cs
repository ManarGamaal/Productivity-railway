using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Filters;

public class TaskQueryFilter
{
    public string UserId { get; set; } = string.Empty;

    public TaskItemStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public Guid? ProjectId { get; set; }

    /// <summary>Inclusive lower bound on DueDate (UTC).</summary>
    public DateTime? DueFromUtc { get; set; }

    /// <summary>Inclusive upper bound on DueDate (UTC).</summary>
    public DateTime? DueToUtc { get; set; }

    public TaskSortField SortBy { get; set; } = TaskSortField.DueDate;
    public bool SortDescending { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
