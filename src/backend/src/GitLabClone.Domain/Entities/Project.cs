using GitLabClone.Domain.Common;
using GitLabClone.Domain.Enums;

namespace GitLabClone.Domain.Entities;

public sealed class Project : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-safe slug. Stored as a plain string in the DB; the Slug value object
    /// is used for creation/validation only. This avoids EF Core owned-type
    /// complexity while keeping domain logic clean.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }
    public ProjectVisibility Visibility { get; set; } = ProjectVisibility.Private;
    public string DefaultBranch { get; set; } = "main";

    /// <summary>
    /// Path to the bare Git repository on local disk.
    /// Format: /repos/{Id}.git
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;

    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public ICollection<ProjectMember> Members { get; set; } = [];
    public ICollection<Issue> Issues { get; set; } = [];
    public ICollection<Label> Labels { get; set; } = [];
    public ICollection<Pipeline> Pipelines { get; set; } = [];
    public ICollection<ActivityEvent> Activities { get; set; } = [];
}
