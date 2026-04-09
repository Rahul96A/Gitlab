using GitLabClone.Domain.Entities;
using GitLabClone.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitLabClone.Infrastructure.Persistence;

/// <summary>
/// Seeds a demo admin account and sample data on startup (Development only).
///
/// Demo credentials:
///   Username: admin
///   Password: Admin@123456
/// </summary>
public sealed class DemoDataSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<DemoDataSeeder> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure database exists and migrations applied
        await db.Database.MigrateAsync(cancellationToken);

        // Seed admin user if not exists
        if (!await db.Users.AnyAsync(u => u.Username == "admin", cancellationToken))
        {
            logger.LogInformation("Seeding demo admin user...");

            var admin = new User
            {
                Username = "admin",
                Email = "admin@gitlabclone.dev",
                DisplayName = "Admin User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
                GlobalRole = MemberRole.Admin,
                IsActive = true
            };

            db.Users.Add(admin);
            await db.SaveChangesAsync(cancellationToken);

            // Create a sample project
            var project = new Project
            {
                Name = "Sample Project",
                Slug = "sample-project",
                Description = "A demo project to showcase GitLabClone features — issues, pipelines, file browsing, and more.",
                Visibility = ProjectVisibility.Public,
                DefaultBranch = "main",
                RepositoryPath = "",  // No actual repo — just for UI demo
                OwnerId = admin.Id
            };

            project.Members.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = admin.Id,
                Role = MemberRole.Maintainer
            });

            db.Projects.Add(project);
            await db.SaveChangesAsync(cancellationToken);

            // Seed labels
            var labels = new[]
            {
                new Label { Name = "bug", Color = "#d73a4a", Description = "Something isn't working", ProjectId = project.Id },
                new Label { Name = "feature", Color = "#0075ca", Description = "New feature request", ProjectId = project.Id },
                new Label { Name = "enhancement", Color = "#a2eeef", Description = "Improvement to existing feature", ProjectId = project.Id },
                new Label { Name = "documentation", Color = "#0e8a16", Description = "Improvements or additions to docs", ProjectId = project.Id },
                new Label { Name = "urgent", Color = "#e4e669", Description = "Requires immediate attention", ProjectId = project.Id },
            };
            db.Labels.AddRange(labels);
            await db.SaveChangesAsync(cancellationToken);

            // Seed sample issues
            var issues = new[]
            {
                new Issue
                {
                    IssueNumber = 1,
                    Title = "Set up CI/CD pipeline for automated deployments",
                    Description = "We need a CI/CD pipeline that builds, tests, and deploys the application to Azure Container Apps automatically on push to main.",
                    Status = IssueStatus.Open,
                    ProjectId = project.Id,
                    AuthorId = admin.Id,
                },
                new Issue
                {
                    IssueNumber = 2,
                    Title = "Add dark mode toggle to header",
                    Description = "Users should be able to toggle between light and dark mode manually, instead of relying solely on system preferences.",
                    Status = IssueStatus.Open,
                    ProjectId = project.Id,
                    AuthorId = admin.Id,
                },
                new Issue
                {
                    IssueNumber = 3,
                    Title = "Fix file browser 404 on nested directories",
                    Description = "Navigating into a subdirectory deeper than 2 levels returns a 404 error. The path parameter encoding seems to be the issue.",
                    Status = IssueStatus.Closed,
                    ProjectId = project.Id,
                    AuthorId = admin.Id,
                },
            };

            // Attach labels
            issues[0].Labels.Add(labels[1]); // feature
            issues[1].Labels.Add(labels[2]); // enhancement
            issues[2].Labels.Add(labels[0]); // bug

            db.Issues.AddRange(issues);
            await db.SaveChangesAsync(cancellationToken);

            // Seed a comment
            db.IssueComments.Add(new IssueComment
            {
                Body = "I've started working on this. The Bicep templates are ready — just need to wire up the GitHub Actions workflow.",
                IssueId = issues[0].Id,
                AuthorId = admin.Id,
            });

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Demo data seeded: admin/Admin@123456 | Sample Project with 3 issues");
        }
        else
        {
            logger.LogInformation("Demo admin already exists, skipping seed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
