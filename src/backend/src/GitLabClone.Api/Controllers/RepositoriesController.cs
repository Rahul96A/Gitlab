using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Api.Controllers;

/// <summary>
/// REST API for repository browsing (file tree, file content, commit log).
/// These endpoints are used by the frontend file browser — they're separate
/// from the Git Smart HTTP endpoints used by the `git` CLI.
/// </summary>
[ApiController]
[Route("api/v1/projects/{slug}/repository")]
public sealed class RepositoriesController(
    IAppDbContext db,
    IGitService gitService,
    ICurrentUserService currentUser
) : ControllerBase
{
    /// <summary>
    /// GET /api/v1/projects/{slug}/repository/tree?ref=main&path=/src
    /// Returns a list of files/directories at the given path and ref.
    /// </summary>
    [HttpGet("tree")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<GitFileEntry>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFileTree(
        string slug,
        [FromQuery(Name = "ref")] string? reference = "main",
        [FromQuery] string? path = null,
        CancellationToken ct = default)
    {
        var project = await ResolveProjectAsync(slug, ct);

        var entries = await gitService.GetFileTreeAsync(
            project.RepositoryPath,
            reference ?? project.DefaultBranch,
            path,
            ct
        );

        return Ok(entries);
    }

    /// <summary>
    /// GET /api/v1/projects/{slug}/repository/files?ref=main&path=src/index.ts
    /// Returns the content of a single file.
    /// </summary>
    [HttpGet("files")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GitFileContent), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileContent(
        string slug,
        [FromQuery] string path,
        [FromQuery(Name = "ref")] string? reference = "main",
        CancellationToken ct = default)
    {
        var project = await ResolveProjectAsync(slug, ct);

        var content = await gitService.GetFileContentAsync(
            project.RepositoryPath,
            reference ?? project.DefaultBranch,
            path,
            ct
        );

        if (content is null)
            return NotFound(new { message = $"File '{path}' not found at ref '{reference}'." });

        return Ok(content);
    }

    /// <summary>
    /// GET /api/v1/projects/{slug}/repository/commits?ref=main&count=20
    /// Returns the commit log for a given ref.
    /// </summary>
    [HttpGet("commits")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<GitCommitInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCommitLog(
        string slug,
        [FromQuery(Name = "ref")] string? reference = "main",
        [FromQuery] int count = 20,
        CancellationToken ct = default)
    {
        var project = await ResolveProjectAsync(slug, ct);

        var commits = await gitService.GetCommitLogAsync(
            project.RepositoryPath,
            reference ?? project.DefaultBranch,
            Math.Clamp(count, 1, 100), // Cap at 100 to prevent abuse
            ct
        );

        return Ok(commits);
    }

    /// <summary>
    /// Resolves a project by slug, checking visibility permissions.
    /// </summary>
    private async Task<Domain.Entities.Project> ResolveProjectAsync(string slug, CancellationToken ct)
    {
        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug, ct)
            ?? throw new NotFoundException("Project", slug);

        // Check visibility
        if (project.Visibility == ProjectVisibility.Private)
        {
            if (!currentUser.IsAuthenticated)
                throw new ForbiddenException("Authentication required for private repositories.");

            var isMember = await db.ProjectMembers.AsNoTracking()
                .AnyAsync(pm => pm.ProjectId == project.Id && pm.UserId == currentUser.UserId, ct);

            if (!isMember)
                throw new ForbiddenException("You don't have access to this repository.");
        }
        else if (project.Visibility == ProjectVisibility.Internal && !currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication required for internal repositories.");
        }

        return project;
    }
}
