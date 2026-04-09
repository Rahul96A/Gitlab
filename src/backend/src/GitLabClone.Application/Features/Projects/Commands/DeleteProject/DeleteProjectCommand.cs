using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Projects.Commands.DeleteProject;

public sealed record DeleteProjectCommand(string Slug) : IRequest;

public sealed class DeleteProjectCommandHandler(
    IProjectRepository projectRepo,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser
) : IRequestHandler<DeleteProjectCommand>
{
    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenException("Must be authenticated.");

        var project = await projectRepo.GetBySlugAsync(request.Slug, cancellationToken)
            ?? throw new NotFoundException("Project", request.Slug);

        if (project.OwnerId != userId)
            throw new ForbiddenException("Only the project owner can delete this project.");

        projectRepo.Remove(project); // Soft-delete via interceptor
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
