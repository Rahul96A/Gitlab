using FluentValidation;
using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Application.Features.Projects.Dtos;
using GitLabClone.Domain.Enums;
using GitLabClone.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Projects.Commands.UpdateProject;

public sealed record UpdateProjectCommand(
    string Slug,
    string? Name,
    string? Description,
    ProjectVisibility? Visibility
) : IRequest<ProjectDto>;

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.Name).MinimumLength(2).MaximumLength(100).When(x => x.Name is not null);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
    }
}

public sealed class UpdateProjectCommandHandler(
    IProjectRepository projectRepo,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IAppDbContext db
) : IRequestHandler<UpdateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Must be authenticated.");

        var project = await projectRepo.GetBySlugAsync(request.Slug, cancellationToken)
            ?? throw new NotFoundException("Project", request.Slug);

        // Only owner or Maintainer+ can update
        var member = await db.ProjectMembers.AsNoTracking()
            .FirstOrDefaultAsync(pm => pm.ProjectId == project.Id && pm.UserId == userId, cancellationToken);

        if (project.OwnerId != userId && (member is null || member.Role < MemberRole.Maintainer))
            throw new ForbiddenException("Only project owner or maintainers can update this project.");

        if (request.Name is not null) project.Name = request.Name;
        if (request.Description is not null) project.Description = request.Description;
        if (request.Visibility.HasValue) project.Visibility = request.Visibility.Value;

        projectRepo.Update(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var owner = await db.Users.FindAsync([project.OwnerId], cancellationToken);
        var memberCount = await db.ProjectMembers.CountAsync(pm => pm.ProjectId == project.Id, cancellationToken);
        var issueCount = await db.Issues.CountAsync(i => i.ProjectId == project.Id, cancellationToken);

        return new ProjectDto(
            project.Id, project.Name, project.Slug, project.Description,
            project.Visibility.ToString(), project.DefaultBranch,
            project.OwnerId, owner?.Username ?? "unknown",
            memberCount, issueCount, project.CreatedAt, project.UpdatedAt
        );
    }
}
