using TaskFlow.Application.DTOs.TimeLogs;

namespace TaskFlow.Application.Interfaces;

public interface ITimeLogService
{
    Task<TimeLogResponse> StartTimerAsync(StartTimerRequest request, CancellationToken ct = default);
    Task<TimeLogResponse> StopTimerAsync(StopTimerRequest? request, CancellationToken ct = default);
    Task<TimeLogResponse?> GetActiveTimerAsync(CancellationToken ct = default);
    Task<TimeLogResponse> CreateManualAsync(ManualTimeLogRequest request, CancellationToken ct = default);
    Task<TimeLogResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TimeLogResponse>> GetByTaskAsync(Guid taskId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
