using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Application.Features.Issues.Dtos;
using GitLabClone.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Issues.Queries.GetIssueComments;

public sealed record GetIssueCommentsQuery(string Slug, int IssueNumber) : IRequest<IReadOnlyList<IssueCommentDto>>;

public sealed class GetIssueCommentsQueryHandler(
    IIssueRepository issueRepo,
    IAppDbContext db
) : IRequestHandler<GetIssueCommentsQuery, IReadOnlyList<IssueCommentDto>>
{
    public async Task<IReadOnlyList<IssueCommentDto>> Handle(GetIssueCommentsQuery request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken)
            ?? throw new NotFoundException("Project", request.Slug);

        var issue = await issueRepo.GetByProjectAndNumberAsync(project.Id, request.IssueNumber, cancellationToken)
            ?? throw new NotFoundException("Issue", $"#{request.IssueNumber}");

        return issue.Comments
            .OrderBy(c => c.CreatedAt)
            .Select(c => new IssueCommentDto(
                c.Id, c.Body, c.AuthorId,
                c.Author?.Username ?? "unknown",
                null, c.CreatedAt
            ))
            .ToList();
    }
}
