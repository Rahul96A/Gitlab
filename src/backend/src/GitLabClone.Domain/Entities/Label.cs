using GitLabClone.Domain.Common;

namespace GitLabClone.Domain.Entities;

public sealed class Label : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Hex color code, e.g. "#FF6B6B"</summary>
    public string Color { get; set; } = "#6B7280";

    public string? Description { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    // Many-to-many with Issues
    public ICollection<Issue> Issues { get; set; } = [];
}
