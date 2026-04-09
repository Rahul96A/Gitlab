using GitLabClone.Domain.Common;
using GitLabClone.Domain.Enums;

namespace GitLabClone.Domain.Entities;

/// <summary>
/// Join entity between Project and User. Tracks per-project role assignments.
/// </summary>
public sealed class ProjectMember : AuditableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public MemberRole Role { get; set; } = MemberRole.Developer;
}
