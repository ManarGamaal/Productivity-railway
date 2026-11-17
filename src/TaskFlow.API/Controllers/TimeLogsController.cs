using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs.TimeLogs;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/timelogs")]
[Produces("application/json")]
public sealed class TimeLogsController : ControllerBase
{
    private readonly ITimeLogService _timeLogService;

    public TimeLogsController(ITimeLogService timeLogService)
    {
        _timeLogService = timeLogService;
    }

    /// <summary>Starts a timer on a task. Fails if the caller already has a running timer.</summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(TimeLogResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TimeLogResponse>> Start([FromBody] StartTimerRequest request, CancellationToken ct)
    {
        var started = await _timeLogService.StartTimerAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = started.Id }, started);
    }

    /// <summary>Stops the caller's running timer.</summary>
    [HttpPost("stop")]
    [ProducesResponseType(typeof(TimeLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TimeLogResponse>> Stop([FromBody] StopTimerRequest? request, CancellationToken ct)
        => Ok(await _timeLogService.StopTimerAsync(request, ct));

    /// <summary>Returns the caller's running timer, or 204 when there is none.</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(TimeLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<TimeLogResponse>> GetActive(CancellationToken ct)
    {
        var active = await _timeLogService.GetActiveTimerAsync(ct);
        return active is null ? NoContent() : Ok(active);
    }

    /// <summary>Logs time manually (StartTime + EndTime, or StartTime + DurationInMinutes).</summary>
    [HttpPost("manual")]
    [ProducesResponseType(typeof(TimeLogResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TimeLogResponse>> CreateManual([FromBody] ManualTimeLogRequest request, CancellationToken ct)
    {
        var created = await _timeLogService.CreateManualAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Gets one time log.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TimeLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TimeLogResponse>> GetById(Guid id, CancellationToken ct)
        => Ok(await _timeLogService.GetByIdAsync(id, ct));

    /// <summary>Lists the time logs of a task (newest first).</summary>
    [HttpGet("~/api/tasks/{taskId:guid}/timelogs")]
    [ProducesResponseType(typeof(IReadOnlyList<TimeLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TimeLogResponse>>> GetByTask(Guid taskId, CancellationToken ct)
        => Ok(await _timeLogService.GetByTaskAsync(taskId, ct));

    /// <summary>Deletes a time log.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _timeLogService.DeleteAsync(id, ct);
        return NoContent();
    }
}
