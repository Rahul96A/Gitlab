using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Application.Common.Models;
using GitLabClone.Application.Features.Pipelines.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Pipelines.Queries.GetPipelines;

public sealed record GetPipelinesQuery(
    string Slug,
    int Page = 1,
    int PageSize = 20
) : IRequest<PaginatedList<PipelineDto>>;

public sealed class GetPipelinesQueryHandler(IAppDbContext db) : IRequestHandler<GetPipelinesQuery, PaginatedList<PipelineDto>>
{
    public async Task<PaginatedList<PipelineDto>> Handle(GetPipelinesQuery request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken)
            ?? throw new NotFoundException("Project", request.Slug);

        var query = db.Pipelines.AsNoTracking()
            .Include(p => p.TriggeredBy)
            .Include(p => p.Jobs)
            .Where(p => p.ProjectId == project.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(p => new PipelineDto(
            p.Id, p.Ref, p.CommitSha, p.Status.ToString(),
            p.StartedAt, p.FinishedAt,
            p.TriggeredBy?.Username ?? "unknown",
            p.Jobs.Count, p.CreatedAt
        )).ToList();

        return new PaginatedList<PipelineDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
