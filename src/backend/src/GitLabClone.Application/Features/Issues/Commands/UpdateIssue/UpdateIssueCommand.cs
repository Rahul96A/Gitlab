using FluentValidation;
using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Application.Features.Issues.Dtos;
using GitLabClone.Domain.Enums;
using GitLabClone.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Issues.Commands.UpdateIssue;

public sealed record UpdateIssueCommand(
    string Slug,
    int IssueNumber,
    string? Title,
    string? Description,
    IssueStatus? Status,
    Guid? AssigneeId
) : IRequest<IssueDto>;

public sealed class UpdateIssueCommandValidator : AbstractValidator<UpdateIssueCommand>
{
    public UpdateIssueCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.IssueNumber).GreaterThan(0);
        RuleFor(x => x.Title).MinimumLength(2).MaximumLength(500).When(x => x.Title is not null);
        RuleFor(x => x.Description).MaximumLength(50_000).When(x => x.Description is not null);
    }
}

public sealed class UpdateIssueCommandHandler(
    IIssueRepository issueRepo,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IAppDbContext db
) : IRequestHandler<UpdateIssueCommand, IssueDto>
{
    public async Task<IssueDto> Handle(UpdateIssueCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Must be authenticated.");

        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken)
            ?? throw new NotFoundException("Project", request.Slug);

        var issue = await issueRepo.GetByProjectAndNumberAsync(project.Id, request.IssueNumber, cancellationToken)
            ?? throw new NotFoundException("Issue", $"#{request.IssueNumber}");

        if (request.Title is not null) issue.Title = request.Title;
        if (request.Description is not null) issue.Description = request.Description;
        if (request.Status.HasValue) issue.Status = request.Status.Value;
        if (request.AssigneeId.HasValue) issue.AssigneeId = request.AssigneeId.Value == Guid.Empty ? null : request.AssigneeId;

        issueRepo.Update(issue);
        await unitOfWork.SaveChangesAsync(cancellationToken);

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
