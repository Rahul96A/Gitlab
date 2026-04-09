namespace GitLabClone.Domain.Common;

/// <summary>
/// Extends BaseEntity with audit timestamps. The EF Core interceptor
/// automatically populates these on SaveChanges — no manual tracking needed.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
