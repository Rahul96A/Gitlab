using GitLabClone.Domain.Common;

namespace GitLabClone.Domain.Events;

public sealed record ProjectCreatedEvent(Guid ProjectId, string Slug, Guid OwnerId) : DomainEvent;
