namespace GitLabClone.Application.Features.Issues.Dtos;

public sealed record IssueDto(
    Guid Id,
    int IssueNumber,
    string Title,
    string? Description,
    string Status,
    Guid ProjectId,
    Guid? AssigneeId,
    string? AssigneeUsername,
    Guid AuthorId,
    string AuthorUsername,
    IReadOnlyList<LabelDto> Labels,
    int CommentCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public sealed record LabelDto(
    Guid Id,
    string Name,
    string Color,
    string? Description
);

public sealed record IssueCommentDto(
    Guid Id,
    string Body,
    Guid AuthorId,
    string AuthorUsername,
    string? AuthorAvatarUrl,
    DateTimeOffset CreatedAt
);
