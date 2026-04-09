using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Features.Projects.Dtos;
using GitLabClone.Domain.Interfaces;
using MediatR;

namespace GitLabClone.Application.Features.Projects.Queries.GetProjectById;

public sealed record GetProjectBySlugQuery(string Slug) : IRequest<ProjectDto>;

public sealed class GetProjectBySlugQueryHandler(
    IProjectRepository projectRepo
) : IRequestHandler<GetProjectBySlugQuery, ProjectDto>
{
    public async Task<ProjectDto> Handle(GetProjectBySlugQuery request, CancellationToken cancellationToken)
    {
        var project = await projectRepo.GetBySlugAsync(request.Slug, cancellationToken)
            ?? throw new NotFoundException("Project", request.Slug);

        return new ProjectDto(
            project.Id, project.Name, project.Slug, project.Description,
            project.Visibility.ToString(), project.DefaultBranch,
            project.OwnerId, project.Owner?.Username ?? "unknown",
            project.Members.Count, project.Issues.Count,
            project.CreatedAt, project.UpdatedAt
        );
    }
}
