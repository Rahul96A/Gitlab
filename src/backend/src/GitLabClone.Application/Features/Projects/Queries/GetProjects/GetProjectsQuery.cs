using GitLabClone.Application.Common.Models;
using GitLabClone.Application.Features.Projects.Dtos;
using GitLabClone.Domain.Interfaces;
using MediatR;
using Mapster;

namespace GitLabClone.Application.Features.Projects.Queries.GetProjects;

public sealed record GetProjectsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null
) : IRequest<PaginatedList<ProjectDto>>;

public sealed class GetProjectsQueryHandler(
    IProjectRepository projectRepo
) : IRequestHandler<GetProjectsQuery, PaginatedList<ProjectDto>>
{
    public async Task<PaginatedList<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await projectRepo.GetPagedAsync(
            request.Page, request.PageSize, request.Search, cancellationToken
        );

        var dtos = items.Select(p => new ProjectDto(
            p.Id, p.Name, p.Slug, p.Description,
            p.Visibility.ToString(), p.DefaultBranch,
            p.OwnerId, p.Owner?.Username ?? "unknown",
            p.Members.Count, p.Issues.Count,
            p.CreatedAt, p.UpdatedAt
        )).ToList();

        return new PaginatedList<ProjectDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
