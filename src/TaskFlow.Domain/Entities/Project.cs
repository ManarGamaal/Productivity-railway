using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class Project : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Hex colour such as "#3B82F6".</summary>
    public string ColorCode { get; set; } = "#3B82F6";

    /// <summary>Owner. Matches ApplicationUser.Id (Identity) without coupling Domain to Identity.</summary>
    public string UserId { get; set; } = string.Empty;

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
