namespace GitLabClone.Application.Features.Projects.Dtos;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string Visibility,
    string DefaultBranch,
    Guid OwnerId,
    string OwnerUsername,
    int MemberCount,
    int IssueCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
