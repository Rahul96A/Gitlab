using FluentValidation;
using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Application.Features.Issues.Dtos;
using GitLabClone.Domain.Entities;
using GitLabClone.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Issues.Commands.AddComment;

public sealed record AddCommentCommand(
    string Slug,
    int IssueNumber,
    string Body
) : IRequest<IssueCommentDto>;

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.IssueNumber).GreaterThan(0);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(50_000);
    }
}

public sealed class AddCommentCommandHandler(
    IIssueRepository issueRepo,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IAppDbContext db
) : IRequestHandler<AddCommentCommand, IssueCommentDto>
{
    public async Task<IssueCommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Must be authenticated.");

        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken)
            ?? throw new NotFoundException("Project", request.Slug);

        var issue = await issueRepo.GetByProjectAndNumberAsync(project.Id, request.IssueNumber, cancellationToken)
            ?? throw new NotFoundException("Issue", $"#{request.IssueNumber}");

        var comment = new IssueComment
        {
            Body = request.Body,
            IssueId = issue.Id,
            AuthorId = userId
        };

        await db.IssueComments.AddAsync(comment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var author = await db.Users.FindAsync([userId], cancellationToken);

        return new IssueCommentDto(
            comment.Id, comment.Body, userId,
            author?.Username ?? "unknown",
            null, comment.CreatedAt
        );
    }
}
