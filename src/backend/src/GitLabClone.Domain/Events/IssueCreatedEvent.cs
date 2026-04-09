using GitLabClone.Domain.Common;

namespace GitLabClone.Domain.Events;

public sealed record IssueCreatedEvent(Guid IssueId, Guid ProjectId, int IssueNumber, Guid AuthorId) : DomainEvent;
