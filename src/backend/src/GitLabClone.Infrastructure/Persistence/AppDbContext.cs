using System.Reflection;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Domain.Common;
using GitLabClone.Domain.Entities;
using GitLabClone.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IMediator mediator
) : DbContext(options), IAppDbContext, IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueComment> IssueComments => Set<IssueComment>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<Pipeline> Pipelines => Set<Pipeline>();
    public DbSet<PipelineJob> PipelineJobs => Set<PipelineJob>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration<T> from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global query filter: hide soft-deleted entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ApplySoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, [modelBuilder]);
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    private static void ApplySoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : class, ISoftDeletable
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    /// <summary>
    /// Overridden SaveChanges to dispatch domain events after persistence.
    /// Events are dispatched AFTER the transaction commits to ensure
    /// side effects only occur when the primary write succeeds.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entities = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear events before saving to prevent duplicate dispatch on retry
        entities.ForEach(e => e.ClearDomainEvents());

        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch events after successful save
        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent, cancellationToken);
        }

        return result;
    }
}
