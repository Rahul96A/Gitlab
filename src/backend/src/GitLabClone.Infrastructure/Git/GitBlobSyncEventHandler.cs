using GitLabClone.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GitLabClone.Infrastructure.Git;

/// <summary>
/// Infrastructure-layer handler that triggers the actual Blob backup
/// when a push event is received. Runs in the background so the push
/// response isn't delayed by the upload.
/// </summary>
public sealed class GitBlobSyncEventHandler(
    IGitBlobSyncService syncService,
    ILogger<GitBlobSyncEventHandler> logger
) : INotificationHandler<RepositoryPushedEvent>
{
    public async Task Handle(RepositoryPushedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Small delay to debounce rapid successive pushes
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            await syncService.BackupRepositoryAsync(notification.ProjectId, cancellationToken);
        }
        catch (Exception ex)
        {
            // Don't let backup failure propagate — the push already succeeded
            logger.LogError(ex, "Failed to backup repo {ProjectId} to Blob after push", notification.ProjectId);
        }
    }
}
