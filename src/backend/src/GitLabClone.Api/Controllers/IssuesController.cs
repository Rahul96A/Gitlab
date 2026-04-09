using GitLabClone.Application.Features.Issues.Commands.AddComment;
using GitLabClone.Application.Features.Issues.Commands.CreateIssue;
using GitLabClone.Application.Features.Issues.Commands.UpdateIssue;
using GitLabClone.Application.Features.Issues.Queries.GetIssueByNumber;
using GitLabClone.Application.Features.Issues.Queries.GetIssueComments;
using GitLabClone.Application.Features.Issues.Queries.GetIssues;
using GitLabClone.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GitLabClone.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{slug}/issues")]
[Authorize]
public sealed class IssuesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetIssues(
        string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetIssuesQuery(slug, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{issueNumber:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetIssue(string slug, int issueNumber, CancellationToken ct)
    {
        var result = await sender.Send(new GetIssueByNumberQuery(slug, issueNumber), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateIssue(string slug, [FromBody] CreateIssueRequest body, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateIssueCommand(slug, body.Title, body.Description, body.AssigneeId, body.LabelIds), ct);
        return CreatedAtAction(nameof(GetIssue), new { slug, issueNumber = result.IssueNumber }, result);
    }

    [HttpPut("{issueNumber:int}")]
    public async Task<IActionResult> UpdateIssue(
        string slug, int issueNumber, [FromBody] UpdateIssueRequest body, CancellationToken ct)
    {
        IssueStatus? status = body.Status is not null
            ? Enum.Parse<IssueStatus>(body.Status, ignoreCase: true)
            : null;

        var result = await sender.Send(
            new UpdateIssueCommand(slug, issueNumber, body.Title, body.Description, status, body.AssigneeId), ct);
        return Ok(result);
    }

    [HttpGet("{issueNumber:int}/comments")]
    [AllowAnonymous]
    public async Task<IActionResult> GetComments(string slug, int issueNumber, CancellationToken ct)
    {
        var result = await sender.Send(new GetIssueCommentsQuery(slug, issueNumber), ct);
        return Ok(result);
    }

    [HttpPost("{issueNumber:int}/comments")]
    public async Task<IActionResult> AddComment(
        string slug, int issueNumber, [FromBody] AddCommentRequest body, CancellationToken ct)
    {
        var result = await sender.Send(new AddCommentCommand(slug, issueNumber, body.Body), ct);
        return CreatedAtAction(nameof(GetComments), new { slug, issueNumber }, result);
    }
}

// Request DTOs (minimal — kept in controller file since they're just wire types)
public sealed record CreateIssueRequest(string Title, string? Description, Guid? AssigneeId, IReadOnlyList<Guid>? LabelIds);
public sealed record UpdateIssueRequest(string? Title, string? Description, string? Status, Guid? AssigneeId);
public sealed record AddCommentRequest(string Body);
