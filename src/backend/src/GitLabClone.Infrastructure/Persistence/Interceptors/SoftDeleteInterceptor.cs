using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GitLabClone.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Converts physical DELETEs to soft deletes for entities implementing ISoftDeletable.
/// Intercepts before SaveChanges and changes EntityState from Deleted to Modified.
/// </summary>
public sealed class SoftDeleteInterceptor(
    ICurrentUserService currentUser
) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in eventData.Context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted)
                continue;

            // Prevent physical delete — convert to soft delete
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = DateTimeOffset.UtcNow;
            entry.Entity.DeletedBy = currentUser.UserId?.ToString();
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
