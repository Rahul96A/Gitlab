using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Application.Features.Issues.Dtos;
using GitLabClone.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Issues.Queries.GetIssueByNumber;

public sealed record GetIssueByNumberQuery(string Slug, int IssueNumber) : IRequest<IssueDto>;

public sealed class GetIssueByNumberQueryHandler(
    IIssueRepository issueRepo,
    IAppDbContext db
) : IRequestHandler<GetIssueByNumberQuery, IssueDto>
{
    public async Task<IssueDto> Handle(GetIssueByNumberQuery request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken)
            ?? throw new NotFoundException("Project", request.Slug);

        var issue = await issueRepo.GetByProjectAndNumberAsync(project.Id, request.IssueNumber, cancellationToken)
            ?? throw new NotFoundException("Issue", $"#{request.IssueNumber}");

        return new IssueDto(
            issue.Id, issue.IssueNumber, issue.Title, issue.Description,
            issue.Status.ToString(), issue.ProjectId,
            issue.AssigneeId, issue.Assignee?.Username,
            issue.AuthorId, issue.Author?.Username ?? "unknown",
            issue.Labels.Select(l => new LabelDto(l.Id, l.Name, l.Color, l.Description)).ToList(),
            issue.Comments.Count, issue.CreatedAt, issue.UpdatedAt
        );
    }
}
