using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GitLabClone.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Automatically sets CreatedAt/UpdatedAt and CreatedBy/UpdatedBy on every
/// save. Runs before SaveChanges so timestamps are part of the transaction.
/// </summary>
public sealed class AuditableEntityInterceptor(
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

        var now = DateTimeOffset.UtcNow;
        var userId = currentUser.UserId?.ToString();

        foreach (var entry in eventData.Context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
