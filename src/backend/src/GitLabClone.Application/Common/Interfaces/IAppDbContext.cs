using GitLabClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Common.Interfaces;

/// <summary>
/// Abstraction over EF Core DbContext exposed to the Application layer.
/// The actual DbContext lives in Infrastructure — this interface prevents
/// the Application layer from depending on EF Core directly.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectMember> ProjectMembers { get; }
    DbSet<Issue> Issues { get; }
    DbSet<IssueComment> IssueComments { get; }
    DbSet<Label> Labels { get; }
    DbSet<Pipeline> Pipelines { get; }
    DbSet<PipelineJob> PipelineJobs { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<ActivityEvent> ActivityEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
