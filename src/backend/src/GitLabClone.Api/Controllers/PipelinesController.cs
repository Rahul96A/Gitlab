using GitLabClone.Application.Features.Pipelines.Commands.TriggerPipeline;
using GitLabClone.Application.Features.Pipelines.Queries.GetPipelineById;
using GitLabClone.Application.Features.Pipelines.Queries.GetPipelineJobs;
using GitLabClone.Application.Features.Pipelines.Queries.GetPipelines;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GitLabClone.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{slug}/pipelines")]
[Authorize]
public sealed class PipelinesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPipelines(
        string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPipelinesQuery(slug, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{pipelineId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPipeline(string slug, Guid pipelineId, CancellationToken ct)
    {
        var result = await sender.Send(new GetPipelineByIdQuery(slug, pipelineId), ct);
        return Ok(result);
    }

    [HttpGet("{pipelineId:guid}/jobs")]
    [AllowAnonymous]
    public async Task<IActionResult> GetJobs(string slug, Guid pipelineId, CancellationToken ct)
    {
        var result = await sender.Send(new GetPipelineJobsQuery(slug, pipelineId), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> TriggerPipeline(string slug, [FromBody] TriggerPipelineRequest body, CancellationToken ct)
    {
        var result = await sender.Send(new TriggerPipelineCommand(slug, body.Ref), ct);
        return CreatedAtAction(nameof(GetPipeline), new { slug, pipelineId = result.Id }, result);
    }
}

public sealed record TriggerPipelineRequest(string Ref);
