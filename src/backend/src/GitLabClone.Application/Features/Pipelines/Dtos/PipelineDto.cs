namespace GitLabClone.Application.Features.Pipelines.Dtos;

public sealed record PipelineDto(
    Guid Id,
    string Ref,
    string CommitSha,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string TriggeredByUsername,
    int JobCount,
    DateTimeOffset CreatedAt
);

public sealed record PipelineJobDto(
    Guid Id,
    string Name,
    string Stage,
    string Status,
    string Log,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? ArtifactUrl
);
