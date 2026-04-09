using GitLabClone.Application.Common.Exceptions;
using GitLabClone.Application.Common.Interfaces;
using GitLabClone.Application.Features.Issues.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Application.Features.Labels.Queries.GetLabels;

public sealed record GetLabelsQuery(string Slug) : IRequest<IReadOnlyList<LabelDto>>;

public sealed class GetLabelsQueryHandler(IAppDbContext db) : IRequestHandler<GetLabelsQuery, IReadOnlyList<LabelDto>>
{
    public async Task<IReadOnlyList<LabelDto>> Handle(GetLabelsQuery request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken)
            ?? throw new NotFoundException("Project", request.Slug);

        return await db.Labels.AsNoTracking()
            .Where(l => l.ProjectId == project.Id)
            .OrderBy(l => l.Name)
            .Select(l => new LabelDto(l.Id, l.Name, l.Color, l.Description))
            .ToListAsync(cancellationToken);
    }
}
