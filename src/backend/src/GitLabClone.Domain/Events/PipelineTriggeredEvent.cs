using GitLabClone.Domain.Common;

namespace GitLabClone.Domain.Events;

public sealed record PipelineTriggeredEvent(Guid PipelineId, Guid ProjectId, string Ref) : DomainEvent;
