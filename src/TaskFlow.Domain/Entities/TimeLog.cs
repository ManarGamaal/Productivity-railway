using TaskFlow.Domain.Common;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Entities;

public class TimeLog : BaseEntity
{
    public Guid TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    /// <summary>Null while the timer is running.</summary>
    public DateTime? EndTime { get; private set; }

    /// <summary>Minutes, rounded to 2 decimals. 0 while the timer is running.</summary>
    public double DurationInMinutes { get; private set; }

    public string? Notes { get; set; }

    public bool IsRunning => EndTime is null;

    /// <summary>Starts a live timer (EndTime stays null until Stop is called).</summary>
    public static TimeLog StartNew(Guid taskItemId, string userId, DateTime startUtc, string? notes)
    {
        return new TimeLog
        {
            TaskItemId = taskItemId,
            UserId = userId,
            StartTime = startUtc,
            Notes = notes
        };
    }

    /// <summary>Creates an already-finished entry (manual logging).</summary>
    public static TimeLog CreateManual(
        Guid taskItemId, string userId, DateTime startUtc, DateTime endUtc, string? notes)
    {
        if (endUtc <= startUtc)
        {
            throw new BusinessRuleException("End time must be later than start time.");
        }

        var log = new TimeLog
        {
            TaskItemId = taskItemId,
            UserId = userId,
            StartTime = startUtc,
            Notes = notes
        };
        log.EndTime = endUtc;
        log.DurationInMinutes = CalculateMinutes(startUtc, endUtc);
        return log;
    }

    /// <summary>Stops a running timer and computes the duration.</summary>
    public void Stop(DateTime endUtc, string? notes = null)
    {
        if (!IsRunning)
        {
            throw new BusinessRuleException("This timer has already been stopped.");
        }

        if (endUtc < StartTime)
        {
            throw new BusinessRuleException("End time cannot be earlier than start time.");
        }

        EndTime = endUtc;
        DurationInMinutes = CalculateMinutes(StartTime, endUtc);

        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = notes;
        }
    }

    private static double CalculateMinutes(DateTime start, DateTime end)
        => Math.Round((end - start).TotalMinutes, 2, MidpointRounding.AwayFromZero);
}
