using GitLabClone.Domain.Common;
using GitLabClone.Domain.Enums;

namespace GitLabClone.Domain.Entities;

public sealed class Pipeline : AuditableEntity
{
    /// <summary>The Git ref (branch/tag) this pipeline runs against.</summary>
    public string Ref { get; set; } = "main";

    /// <summary>Commit SHA that triggered the pipeline.</summary>
    public string CommitSha { get; set; } = string.Empty;

    public PipelineStatus Status { get; set; } = PipelineStatus.Pending;

    /// <summary>Raw YAML content of the CI config at trigger time.</summary>
    public string YamlContent { get; set; } = string.Empty;

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid TriggeredById { get; set; }
    public User TriggeredBy { get; set; } = null!;

    public ICollection<PipelineJob> Jobs { get; set; } = [];
}
