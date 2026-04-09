using GitLabClone.Domain.Entities;
using GitLabClone.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository(AppDbContext db) : IProjectRepository
{
    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Projects
            .Include(p => p.Owner)
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Project?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        await db.Projects
            .Include(p => p.Owner)
            .Include(p => p.Members)
            .Include(p => p.Issues)
            .FirstOrDefaultAsync(p => p.Slug == slug, ct);

    public async Task<(IReadOnlyList<Project> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var query = db.Projects
            .Include(p => p.Owner)
            .Include(p => p.Members)
            .Include(p => p.Issues)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLowerInvariant();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term))
            );
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(Project project, CancellationToken ct = default) =>
        await db.Projects.AddAsync(project, ct);

    public void Update(Project project) =>
        db.Projects.Update(project);

    public void Remove(Project project) =>
        db.Projects.Remove(project); // SoftDeleteInterceptor will convert this

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        await db.Projects.AnyAsync(p => p.Slug == slug, ct);
}
