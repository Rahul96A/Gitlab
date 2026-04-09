using GitLabClone.Domain.Entities;
using GitLabClone.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GitLabClone.Infrastructure.Persistence.Repositories;

public sealed class IssueRepository(AppDbContext db) : IIssueRepository
{
    public async Task<Issue?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Issues
            .Include(i => i.Author)
            .Include(i => i.Assignee)
            .Include(i => i.Labels)
            .Include(i => i.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<Issue?> GetByProjectAndNumberAsync(Guid projectId, int issueNumber, CancellationToken ct = default) =>
        await db.Issues
            .Include(i => i.Author)
            .Include(i => i.Assignee)
            .Include(i => i.Labels)
            .Include(i => i.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(i => i.ProjectId == projectId && i.IssueNumber == issueNumber, ct);

    public async Task<(IReadOnlyList<Issue> Items, int TotalCount)> GetPagedByProjectAsync(
        Guid projectId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Issues
            .Include(i => i.Author)
            .Include(i => i.Assignee)
            .Include(i => i.Labels)
            .Where(i => i.ProjectId == projectId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(Issue issue, CancellationToken ct = default) =>
        await db.Issues.AddAsync(issue, ct);

    public void Update(Issue issue) =>
        db.Issues.Update(issue);

    public async Task<int> GetNextIssueNumberAsync(Guid projectId, CancellationToken ct = default)
    {
        var max = await db.Issues
            .IgnoreQueryFilters() // Include soft-deleted to avoid number reuse
            .Where(i => i.ProjectId == projectId)
            .MaxAsync(i => (int?)i.IssueNumber, ct);

        return (max ?? 0) + 1;
    }
}
