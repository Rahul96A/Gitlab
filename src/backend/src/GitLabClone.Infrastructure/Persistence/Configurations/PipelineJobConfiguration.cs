using GitLabClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GitLabClone.Infrastructure.Persistence.Configurations;

public sealed class PipelineJobConfiguration : IEntityTypeConfiguration<PipelineJob>
{
    public void Configure(EntityTypeBuilder<PipelineJob> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Name).HasMaxLength(255).IsRequired();
        builder.Property(j => j.Stage).HasMaxLength(100).IsRequired();
        builder.Property(j => j.ArtifactUrl).HasMaxLength(2048);
    }
}
