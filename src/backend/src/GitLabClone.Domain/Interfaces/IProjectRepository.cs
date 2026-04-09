using GitLabClone.Domain.Entities;

namespace GitLabClone.Domain.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Project?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<(IReadOnlyList<Project> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task AddAsync(Project project, CancellationToken ct = default);
    void Update(Project project);
    void Remove(Project project);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
}
