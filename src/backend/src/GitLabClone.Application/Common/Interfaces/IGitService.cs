namespace GitLabClone.Application.Common.Interfaces;

/// <summary>
/// Abstracts all Git operations. Phase 3 implements this with LibGit2Sharp.
/// </summary>
public interface IGitService
{
    Task<string> InitBareRepositoryAsync(Guid projectId, string defaultBranch, CancellationToken ct = default);
    Task<IReadOnlyList<GitFileEntry>> GetFileTreeAsync(string repoPath, string reference, string? path, CancellationToken ct = default);
    Task<GitFileContent?> GetFileContentAsync(string repoPath, string reference, string filePath, CancellationToken ct = default);
    Task<IReadOnlyList<GitCommitInfo>> GetCommitLogAsync(string repoPath, string reference, int maxCount = 20, CancellationToken ct = default);
}

public record GitFileEntry(string Name, string Path, string Type, long Size); // Type: "blob" or "tree"
public record GitFileContent(string Path, string Content, long Size, string Encoding);
public record GitCommitInfo(string Sha, string Message, string AuthorName, string AuthorEmail, DateTimeOffset Timestamp);
