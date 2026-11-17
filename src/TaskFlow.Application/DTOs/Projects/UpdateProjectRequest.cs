namespace TaskFlow.Application.DTOs.Projects;

public sealed class UpdateProjectRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Hex colour (#RGB or #RRGGBB). Defaults to #3B82F6.</summary>
    public string? ColorCode { get; set; }
}
