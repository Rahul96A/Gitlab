using GitLabClone.Domain.Common;

namespace GitLabClone.Domain.Entities;

/// <summary>
/// Immutable audit log / activity feed entry. Never updated or deleted.
/// </summary>
public sealed class ActivityEvent : BaseEntity
{
    public string Action { get; init; } = string.Empty;       // "created_issue", "pushed", "merged", etc.
    public string TargetType { get; init; } = string.Empty;   // "Issue", "Pipeline", "Project"
    public Guid TargetId { get; init; }
    public string? TargetTitle { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    public Guid UserId { get; init; }
    public User User { get; init; } = null!;

    public Guid ProjectId { get; init; }
    public Project Project { get; init; } = null!;
}
