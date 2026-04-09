using GitLabClone.Domain.Common;

namespace GitLabClone.Domain.Entities;

public sealed class IssueComment : AuditableEntity
{
    public string Body { get; set; } = string.Empty;

    public Guid IssueId { get; set; }
    public Issue Issue { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
}
