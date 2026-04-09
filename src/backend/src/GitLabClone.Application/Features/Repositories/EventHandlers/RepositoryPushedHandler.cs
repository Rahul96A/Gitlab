using GitLabClone.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GitLabClone.Application.Features.Repositories.EventHandlers;

/// <summary>
/// Handles post-push backup to Blob storage.
/// Uses INotificationHandler so it runs asynchronously after SaveChanges.
/// We reference IGitBlobSyncService by its interface — the implementation
/// lives in Infrastructure.
/// </summary>
public sealed class RepositoryPushedHandler(
    ILogger<RepositoryPushedHandler> logger
) : INotificationHandler<RepositoryPushedEvent>
{
    public Task Handle(RepositoryPushedEvent notification, CancellationToken cancellationToken)
    {
        // Note: Actual backup call is made by the Infrastructure layer's event handler
        // (see GitBlobSyncEventHandler.cs). This Application-layer handler is for
        // logging and any cross-cutting application logic like activity feed updates.
        logger.LogInformation(
            "Push event received for project {ProjectId} on ref {Ref} by user {UserId}",
            notification.ProjectId, notification.Ref, notification.PushedByUserId
        );

        return Task.CompletedTask;
    }
}
