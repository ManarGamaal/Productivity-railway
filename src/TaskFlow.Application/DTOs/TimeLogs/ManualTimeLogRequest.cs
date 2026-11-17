namespace TaskFlow.Application.DTOs.TimeLogs;

/// <summary>
/// Manual entry. Provide StartTime and EITHER EndTime OR DurationInMinutes (not both).
/// </summary>
public sealed class ManualTimeLogRequest
{
    public Guid TaskId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double? DurationInMinutes { get; set; }
    public string? Notes { get; set; }
}
