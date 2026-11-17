using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs.Analytics;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/analytics")]
[Produces("application/json")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Daily summary: tasks completed, time tracked, breakdown by project and completion rate.
    /// </summary>
    /// <param name="date">UTC day in YYYY-MM-DD format. Defaults to today (UTC).</param>
    [HttpGet("daily")]
    [ProducesResponseType(typeof(DailySummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DailySummaryResponse>> GetDaily([FromQuery] DateOnly? date, CancellationToken ct)
        => Ok(await _analyticsService.GetDailySummaryAsync(date, ct));

    /// <summary>
    /// Weekly summary: 7-day breakdown, most productive day and total hours logged.
    /// </summary>
    /// <param name="startDate">First UTC day in YYYY-MM-DD format. Defaults to Monday of the current week.</param>
    [HttpGet("weekly")]
    [ProducesResponseType(typeof(WeeklySummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<WeeklySummaryResponse>> GetWeekly([FromQuery] DateOnly? startDate, CancellationToken ct)
        => Ok(await _analyticsService.GetWeeklySummaryAsync(startDate, ct));
}
