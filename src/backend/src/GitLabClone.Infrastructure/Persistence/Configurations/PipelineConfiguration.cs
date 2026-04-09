using GitLabClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GitLabClone.Infrastructure.Persistence.Configurations;

public sealed class PipelineConfiguration : IEntityTypeConfiguration<Pipeline>
{
    public void Configure(EntityTypeBuilder<Pipeline> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Ref).HasMaxLength(256).IsRequired();
        builder.Property(p => p.CommitSha).HasMaxLength(40).IsRequired();
        builder.Property(p => p.YamlContent).IsRequired();

        builder.HasOne(p => p.TriggeredBy)
            .WithMany()
            .HasForeignKey(p => p.TriggeredById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Jobs)
            .WithOne(j => j.Pipeline)
            .HasForeignKey(j => j.PipelineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
