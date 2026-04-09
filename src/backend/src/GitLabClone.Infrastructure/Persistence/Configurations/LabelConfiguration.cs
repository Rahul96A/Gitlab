using GitLabClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GitLabClone.Infrastructure.Persistence.Configurations;

public sealed class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name).HasMaxLength(50).IsRequired();
        builder.Property(l => l.Color).HasMaxLength(7).IsRequired();
        builder.Property(l => l.Description).HasMaxLength(255);

        builder.HasIndex(l => new { l.ProjectId, l.Name }).IsUnique();
    }
}
