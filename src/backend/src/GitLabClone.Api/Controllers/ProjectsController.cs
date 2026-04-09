using GitLabClone.Application.Features.Projects.Commands.CreateProject;
using GitLabClone.Application.Features.Projects.Commands.DeleteProject;
using GitLabClone.Application.Features.Projects.Commands.UpdateProject;
using GitLabClone.Application.Features.Projects.Queries.GetProjectById;
using GitLabClone.Application.Features.Projects.Queries.GetProjects;
using GitLabClone.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GitLabClone.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
public sealed class ProjectsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProjects(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetProjectsQuery(page, pageSize, search), ct);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProject(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetProjectBySlugQuery(slug), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return CreatedAtAction(nameof(GetProject), new { slug = result.Slug }, result);
    }

    [HttpPut("{slug}")]
    public async Task<IActionResult> UpdateProject(
        string slug, [FromBody] UpdateProjectRequest body, CancellationToken ct)
    {
        ProjectVisibility? visibility = body.Visibility is not null
            ? Enum.Parse<ProjectVisibility>(body.Visibility, ignoreCase: true)
            : null;

        var result = await sender.Send(
            new UpdateProjectCommand(slug, body.Name, body.Description, visibility), ct);
        return Ok(result);
    }

    [HttpDelete("{slug}")]
    public async Task<IActionResult> DeleteProject(string slug, CancellationToken ct)
    {
        await sender.Send(new DeleteProjectCommand(slug), ct);
        return NoContent();
    }
}

public sealed record UpdateProjectRequest(string? Name, string? Description, string? Visibility);
