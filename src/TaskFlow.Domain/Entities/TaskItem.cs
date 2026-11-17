using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

public class TaskItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public TaskItemStatus Status { get; private set; } = TaskItemStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>Set automatically when Status becomes Completed; cleared otherwise.</summary>
    public DateTime? CompletedAtUtc { get; private set; }

    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ICollection<TimeLog> TimeLogs { get; set; } = new List<TimeLog>();

    /// <summary>
    /// Single place that owns the Status/CompletedAtUtc invariant.
    /// </summary>
    public void ChangeStatus(TaskItemStatus newStatus, DateTime utcNow)
    {
        if (Status == newStatus)
        {
            return;
        }

        Status = newStatus;
        CompletedAtUtc = newStatus == TaskItemStatus.Completed ? utcNow : null;
    }
}
