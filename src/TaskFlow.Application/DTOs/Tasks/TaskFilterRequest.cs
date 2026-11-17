using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.DTOs.Tasks;

public sealed class TaskFilterRequest
{
    public TaskItemStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public Guid? ProjectId { get; set; }

    /// <summary>Inclusive lower bound of the due date.</summary>
    public DateTime? DueFrom { get; set; }

    /// <summary>Inclusive upper bound of the due date.</summary>
    public DateTime? DueTo { get; set; }

    public TaskSortField SortBy { get; set; } = TaskSortField.DueDate;
    public bool SortDescending { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
