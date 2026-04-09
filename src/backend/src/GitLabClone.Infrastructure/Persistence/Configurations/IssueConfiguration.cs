using GitLabClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GitLabClone.Infrastructure.Persistence.Configurations;

public sealed class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.HasKey(i => i.Id);

        // Unique issue number within a project
        builder.HasIndex(i => new { i.ProjectId, i.IssueNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Property(i => i.Title).HasMaxLength(255).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(10000);

        builder.HasOne(i => i.Author)
            .WithMany()
            .HasForeignKey(i => i.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Assignee)
            .WithMany(u => u.AssignedIssues)
            .HasForeignKey(i => i.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(i => i.Comments)
            .WithOne(c => c.Issue)
            .HasForeignKey(c => c.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-many: Issue <-> Label (EF Core auto join table)
        builder.HasMany(i => i.Labels)
            .WithMany(l => l.Issues)
            .UsingEntity("IssueLabels");
    }
}
