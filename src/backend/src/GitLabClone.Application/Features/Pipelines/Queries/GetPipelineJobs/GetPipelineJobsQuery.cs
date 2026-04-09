using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Application.Features.Pipelines.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Pipelines.Queries.GetPipelineJobs;

public sealed record GetPipelineJobsQuery(string Slug, Guid PipelineId) : IRequest<IReadOnlyList<PipelineJobDto>>;

public sealed class GetPipelineJobsQueryHandler(IAppDbContext db) : IRequestHandler<GetPipelineJobsQuery, IReadOnlyList<PipelineJobDto>>
{
    public async Task<IReadOnlyList<PipelineJobDto>> Handle(GetPipelineJobsQuery request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken)
            ?? throw new NotFoundException("Project", request.Slug);

        var pipeline = await db.Pipelines.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PipelineId && p.ProjectId == project.Id, cancellationToken)
            ?? throw new NotFoundException("Pipeline", request.PipelineId);

        return await db.PipelineJobs.AsNoTracking()
            .Where(j => j.PipelineId == pipeline.Id)
            .OrderBy(j => j.Stage).ThenBy(j => j.Name)
            .Select(j => new PipelineJobDto(
                j.Id, j.Name, j.Stage, j.Status.ToString(),
                j.Log, j.StartedAt, j.FinishedAt, j.ArtifactUrl
            ))
            .ToListAsync(cancellationToken);
    }
}
