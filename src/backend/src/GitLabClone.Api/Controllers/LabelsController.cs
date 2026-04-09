using GitLabClone.Application.Features.Labels.Commands.CreateLabel;
using GitLabClone.Application.Features.Labels.Queries.GetLabels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GitLabClone.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{slug}/labels")]
[Authorize]
public sealed class LabelsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetLabels(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetLabelsQuery(slug), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateLabel(string slug, [FromBody] CreateLabelRequest body, CancellationToken ct)
    {
        var result = await sender.Send(new CreateLabelCommand(slug, body.Name, body.Color, body.Description), ct);
        return Created($"api/v1/projects/{slug}/labels", result);
    }
}

public sealed record CreateLabelRequest(string Name, string Color, string? Description);
