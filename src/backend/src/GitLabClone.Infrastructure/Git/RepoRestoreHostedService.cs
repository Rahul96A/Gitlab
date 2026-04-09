using GitLabClone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitLabClone.Infrastructure.Git;

/// <summary>
/// Runs once on application startup. Checks all projects in the database
/// and restores any missing bare repos from Azure Blob Storage.
///
/// This ensures that after a container restart or scale-out event,
/// all repositories are available on local disk before Git traffic
/// reaches the server.
/// </summary>
public sealed class RepoRestoreHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<RepoRestoreHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting repository restore check...");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var syncService = scope.ServiceProvider.GetRequiredService<IGitBlobSyncService>();

        var projects = await db.Projects
            .AsNoTracking()
            .Select(p => new { p.Id, p.RepositoryPath })
            .ToListAsync(cancellationToken);

        if (projects.Count == 0)
        {
            logger.LogInformation("No projects found. Skipping restore.");
            return;
        }

        var tuples = projects.Select(p => (p.Id, p.RepositoryPath));
        await syncService.RestoreAllMissingAsync(tuples, cancellationToken);

        logger.LogInformation("Repository restore check complete. {Count} projects verified.", projects.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
