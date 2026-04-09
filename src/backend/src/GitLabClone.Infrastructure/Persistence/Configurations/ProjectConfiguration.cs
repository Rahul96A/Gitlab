using GitLabClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GitLabClone.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();

        builder.Property(p => p.Slug).HasMaxLength(64).IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.DefaultBranch).HasMaxLength(256).HasDefaultValue("main");
        builder.Property(p => p.RepositoryPath).HasMaxLength(500).IsRequired();

        builder.HasOne(p => p.Owner)
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Members)
            .WithOne(m => m.Project)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Issues)
            .WithOne(i => i.Project)
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Labels)
            .WithOne(l => l.Project)
            .HasForeignKey(l => l.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Pipelines)
            .WithOne(pl => pl.Project)
            .HasForeignKey(pl => pl.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Activities)
            .WithOne(a => a.Project)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
