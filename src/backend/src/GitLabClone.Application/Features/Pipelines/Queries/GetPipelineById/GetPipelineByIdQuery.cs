using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Application.Features.Pipelines.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Pipelines.Queries.GetPipelineById;

public sealed record GetPipelineByIdQuery(string Slug, Guid PipelineId) : IRequest<PipelineDto>;

public sealed class GetPipelineByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetPipelineByIdQuery, PipelineDto>
{
    public async Task<PipelineDto> Handle(GetPipelineByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken)
            ?? throw new NotFoundException("Project", request.Slug);

        var pipeline = await db.Pipelines.AsNoTracking()
            .Include(p => p.TriggeredBy)
            .Include(p => p.Jobs)
            .FirstOrDefaultAsync(p => p.Id == request.PipelineId && p.ProjectId == project.Id, cancellationToken)
            ?? throw new NotFoundException("Pipeline", request.PipelineId);

        return new PipelineDto(
            pipeline.Id, pipeline.Ref, pipeline.CommitSha,
            pipeline.Status.ToString(),
            pipeline.StartedAt, pipeline.FinishedAt,
            pipeline.TriggeredBy?.Username ?? "unknown",
            pipeline.Jobs.Count, pipeline.CreatedAt
        );
    }
}
