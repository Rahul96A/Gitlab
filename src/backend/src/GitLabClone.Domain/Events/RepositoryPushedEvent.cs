using GitLabClone.Domain.Common;

namespace GitLabClone.Domain.Events;

/// <summary>
/// Raised after a successful git-receive-pack (push) completes.
/// Triggers backup to Azure Blob Storage and activity feed update.
/// </summary>
public sealed record RepositoryPushedEvent(
    Guid ProjectId,
    string Ref,
    Guid PushedByUserId
) : DomainEvent;
