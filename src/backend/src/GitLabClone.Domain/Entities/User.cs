using GitLabClone.Domain.Common;
using GitLabClone.Domain.Enums;

namespace GitLabClone.Domain.Entities;

public sealed class User : AuditableEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Entra ID object ID — null for local-only accounts.
    /// When set, the user authenticated via Microsoft Entra ID (SSO).
    /// </summary>
    public string? EntraObjectId { get; set; }

    public MemberRole GlobalRole { get; set; } = MemberRole.Developer;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = [];
    public ICollection<Issue> AssignedIssues { get; set; } = [];
    public ICollection<IssueComment> Comments { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
}
