namespace GitLabClone.Domain.Common;

/// <summary>
/// Marker interface for entities that support soft-delete.
/// The SoftDeleteInterceptor sets these on delete instead of removing the row.
/// A global query filter in EF Core hides soft-deleted records by default.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
