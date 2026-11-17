namespace TaskFlow.Application.DTOs.TimeLogs;

public sealed class StartTimerRequest
{
    public Guid TaskId { get; set; }
    public string? Notes { get; set; }
}
