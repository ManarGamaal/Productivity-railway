using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.DTOs.Tasks;

public sealed class ChangeTaskStatusRequest
{
    public TaskItemStatus Status { get; set; }
}
