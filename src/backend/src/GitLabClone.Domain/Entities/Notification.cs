using GitLabClone.Domain.Common;

namespace GitLabClone.Domain.Entities;

public sealed class Notification : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Polymorphic link: "Issue", "Pipeline", "Project", etc.
    /// Combined with TargetId to link to the source entity.
    /// </summary>
    public string TargetType { get; set; } = string.Empty;
    public Guid TargetId { get; set; }

    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
