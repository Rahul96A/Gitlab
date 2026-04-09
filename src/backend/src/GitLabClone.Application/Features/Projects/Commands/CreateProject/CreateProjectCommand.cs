using FluentValidation;
using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Application.Features.Projects.Dtos;
using GitLabClone.Domain.Entities;
using GitLabClone.Domain.Enums;
using GitLabClone.Domain.Events;
using GitLabClone.Domain.Interfaces;
using GitLabClone.Domain.ValueObjects;
using MediatR;

namespace GitLabClone.Application.Features.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(
    string Name,
    string? Description,
    ProjectVisibility Visibility = ProjectVisibility.Private,
    string DefaultBranch = "main"
) : IRequest<ProjectDto>;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Visibility).IsInEnum();
        RuleFor(x => x.DefaultBranch).NotEmpty().MaximumLength(256);
    }
}

public sealed class CreateProjectCommandHandler(
    IProjectRepository projectRepo,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IGitService gitService,
    IAppDbContext db
) : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Must be authenticated to create a project.");

        var slug = Slug.Create(request.Name);

        if (await projectRepo.SlugExistsAsync(slug.Value, cancellationToken))
            throw new Common.Exceptions.ValidationException(
                [new FluentValidation.Results.ValidationFailure("Name", $"A project with slug '{slug}' already exists.")]
            );

        var repoPath = await gitService.InitBareRepositoryAsync(Guid.CreateVersion7(), request.DefaultBranch, cancellationToken);

        var project = new Project
        {
            Name = request.Name,
            Slug = slug.Value,
            Description = request.Description,
            Visibility = request.Visibility,
            DefaultBranch = request.DefaultBranch,
            RepositoryPath = repoPath,
            OwnerId = userId
        };

        // Auto-add owner as Maintainer
        project.Members.Add(new ProjectMember
        {
            ProjectId = project.Id,
            UserId = userId,
            Role = MemberRole.Maintainer
        });

        project.AddDomainEvent(new ProjectCreatedEvent(project.Id, project.Slug, userId));

        await projectRepo.AddAsync(project, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await db.Users.FindAsync([userId], cancellationToken);

        return new ProjectDto(
            project.Id, project.Name, project.Slug, project.Description,
            project.Visibility.ToString(), project.DefaultBranch,
            userId, user?.Username ?? "unknown",
            1, 0, project.CreatedAt, project.UpdatedAt
        );
    }
}
