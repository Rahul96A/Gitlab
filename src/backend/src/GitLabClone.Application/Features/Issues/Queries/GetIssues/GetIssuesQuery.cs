using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Application.Common.Models;
using GitLabClone.Application.Features.Issues.Dtos;
using GitLabClone.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Issues.Queries.GetIssues;

public sealed record GetIssuesQuery(
    string Slug,
    int Page = 1,
    int PageSize = 20
) : IRequest<PaginatedList<IssueDto>>;

public sealed class GetIssuesQueryHandler(
    IIssueRepository issueRepo,
    IAppDbContext db
) : IRequestHandler<GetIssuesQuery, PaginatedList<IssueDto>>
{
    public async Task<PaginatedList<IssueDto>> Handle(GetIssuesQuery request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken)
            ?? throw new NotFoundException("Project", request.Slug);

        var (items, totalCount) = await issueRepo.GetPagedByProjectAsync(
            project.Id, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(i => new IssueDto(
            i.Id, i.IssueNumber, i.Title, i.Description,
            i.Status.ToString(), i.ProjectId,
            i.AssigneeId, i.Assignee?.Username,
            i.AuthorId, i.Author?.Username ?? "unknown",
            i.Labels.Select(l => new LabelDto(l.Id, l.Name, l.Color, l.Description)).ToList(),
            i.Comments.Count, i.CreatedAt, i.UpdatedAt
        )).ToList();

        return new PaginatedList<IssueDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
