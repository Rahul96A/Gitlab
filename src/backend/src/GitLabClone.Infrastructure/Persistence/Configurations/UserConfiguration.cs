using GitLabClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GitLabClone.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username).HasMaxLength(39).IsRequired();
        builder.HasIndex(u => u.Username).IsUnique();

        builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.AvatarUrl).HasMaxLength(2048);
        builder.Property(u => u.EntraObjectId).HasMaxLength(36);

        builder.HasIndex(u => u.EntraObjectId)
            .IsUnique()
            .HasFilter("[EntraObjectId] IS NOT NULL");
    }
}
