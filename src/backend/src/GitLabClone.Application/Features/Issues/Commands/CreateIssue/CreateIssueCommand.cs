using FluentValidation;
using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Application.Features.Issues.Dtos;
using GitLabClone.Domain.Entities;
using GitLabClone.Domain.Events;
using GitLabClone.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Issues.Commands.CreateIssue;

public sealed record CreateIssueCommand(
    string Slug,
    string Title,
    string? Description,
    Guid? AssigneeId,
    IReadOnlyList<Guid>? LabelIds
) : IRequest<IssueDto>;

public sealed class CreateIssueCommandValidator : AbstractValidator<CreateIssueCommand>
{
    public CreateIssueCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MinimumLength(2).MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(50_000);
    }
}

public sealed class CreateIssueCommandHandler(
    IIssueRepository issueRepo,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IAppDbContext db
) : IRequestHandler<CreateIssueCommand, IssueDto>
{
    public async Task<IssueDto> Handle(CreateIssueCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Must be authenticated.");

        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken)
            ?? throw new NotFoundException("Project", request.Slug);

        var issueNumber = await issueRepo.GetNextIssueNumberAsync(project.Id, cancellationToken);

        var issue = new Issue
        {
            IssueNumber = issueNumber,
            Title = request.Title,
            Description = request.Description,
            ProjectId = project.Id,
            AssigneeId = request.AssigneeId,
            AuthorId = userId
        };

        // Attach labels
        if (request.LabelIds is { Count: > 0 })
        {
            var labels = await db.Labels
                .Where(l => l.ProjectId == project.Id && request.LabelIds.Contains(l.Id))
                .ToListAsync(cancellationToken);
            foreach (var label in labels)
                issue.Labels.Add(label);
        }

        issue.AddDomainEvent(new IssueCreatedEvent(issue.Id, project.Id, issueNumber, userId));

        await issueRepo.AddAsync(issue, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var author = await db.Users.FindAsync([userId], cancellationToken);
        var assignee = request.AssigneeId.HasValue
            ? await db.Users.FindAsync([request.AssigneeId.Value], cancellationToken)
            : null;

        return new IssueDto(
            issue.Id, issue.IssueNumber, issue.Title, issue.Description,
            issue.Status.ToString(), issue.ProjectId,
            issue.AssigneeId, assignee?.Username,
            userId, author?.Username ?? "unknown",
            issue.Labels.Select(l => new LabelDto(l.Id, l.Name, l.Color, l.Description)).ToList(),
            0, issue.CreatedAt, issue.UpdatedAt
        );
    }
}
