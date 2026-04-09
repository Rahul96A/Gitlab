using GitLabClone.Domain.Entities;

namespace GitLabClone.Domain.Interfaces;

public interface IIssueRepository
{
    Task<Issue?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Issue?> GetByProjectAndNumberAsync(Guid projectId, int issueNumber, CancellationToken ct = default);
    Task<(IReadOnlyList<Issue> Items, int TotalCount)> GetPagedByProjectAsync(Guid projectId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Issue issue, CancellationToken ct = default);
    void Update(Issue issue);
    Task<int> GetNextIssueNumberAsync(Guid projectId, CancellationToken ct = default);
}
