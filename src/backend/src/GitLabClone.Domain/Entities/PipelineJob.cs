using GitLabClone.Domain.Common;
using GitLabClone.Domain.Enums;

namespace GitLabClone.Domain.Entities;

public sealed class PipelineJob : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public JobStatus Status { get; set; } = JobStatus.Pending;

    /// <summary>Simulated job output log.</summary>
    public string Log { get; set; } = string.Empty;

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>Optional Azure Blob URL to stored artifacts.</summary>
    public string? ArtifactUrl { get; set; }

    public Guid PipelineId { get; set; }
    public Pipeline Pipeline { get; set; } = null!;
}
