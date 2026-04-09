using System.Text;
using GitLabClone.Application.Common.Interfaces;
using LibGit2Sharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GitLabClone.Infrastructure.Git;

/// <summary>
/// Full LibGit2Sharp implementation for Git operations on bare repositories.
///
/// Architecture note: LibGit2Sharp is NOT thread-safe per Repository instance.
/// Each method opens its own Repository handle and disposes it when done.
/// This is safe for concurrent web requests because each call gets its own handle,
/// and libgit2 uses file-level locks for writes (push/receive).
///
/// All repos are stored as bare repos (no working directory) because:
/// 1. Server-side repos don't need checked-out files
/// 2. Bare repos are smaller and faster to sync to Blob storage
/// 3. Git HTTP protocol only needs the object store + refs
/// </summary>
public sealed class GitService(
    IConfiguration configuration,
    ILogger<GitService> logger
) : IGitService
{
    private readonly string _basePath = Path.GetFullPath(
        configuration["Git:RepoBasePath"]
        ?? Path.Combine(Path.GetTempPath(), "gitlabclone-repos")
    );

    /// <summary>
    /// Initializes a new bare Git repository with an initial commit on the default branch.
    /// The initial commit creates a README.md so the repo isn't empty — this prevents
    /// confusing "empty repository" states in the UI and lets clone work immediately.
    /// </summary>
    public Task<string> InitBareRepositoryAsync(Guid projectId, string defaultBranch, CancellationToken ct = default)
    {
        var repoPath = Path.Combine(_basePath, $"{projectId}.git");
        Directory.CreateDirectory(repoPath);

        // Init bare repo
        Repository.Init(repoPath, isBare: true);

        // Create initial commit with a README so the default branch exists.
        // LibGit2Sharp requires a temporary non-bare clone to commit into a bare repo,
        // OR we can build the tree + commit objects directly against the object database.
        using var repo = new Repository(repoPath);

        // Build a blob for README.md
        var readmeContent = $"# New Project\n\nWelcome to your new project.\n";
        var blob = repo.ObjectDatabase.CreateBlob(new MemoryStream(Encoding.UTF8.GetBytes(readmeContent)));

        // Build a tree containing the README
        var treeDefinition = new TreeDefinition();
        treeDefinition.Add("README.md", blob, Mode.NonExecutableFile);
        var tree = repo.ObjectDatabase.CreateTree(treeDefinition);

        // Create the initial commit (no parents)
        var author = new Signature("GitLabClone", "system@gitlabclone.local", DateTimeOffset.UtcNow);
        var commit = repo.ObjectDatabase.CreateCommit(
            author, author,
            "Initial commit\n",
            tree,
            parents: [],
            prettifyMessage: true
        );

        // Point HEAD and the default branch at this commit
        repo.Refs.UpdateTarget("HEAD", $"refs/heads/{defaultBranch}");
        repo.Refs.Add($"refs/heads/{defaultBranch}", commit.Id);

        logger.LogInformation("Initialized bare repo at {RepoPath} with branch {Branch}", repoPath, defaultBranch);
        return Task.FromResult(repoPath);
    }

    /// <summary>
    /// Returns the file/directory listing at a given path and ref.
    /// If path is null/empty, returns the root tree.
    /// </summary>
    public Task<IReadOnlyList<GitFileEntry>> GetFileTreeAsync(
        string repoPath, string reference, string? path, CancellationToken ct = default)
    {
        using var repo = new Repository(repoPath);

        var commit = ResolveCommit(repo, reference);
        if (commit is null)
            return Task.FromResult<IReadOnlyList<GitFileEntry>>([]);

        var tree = commit.Tree;

        // Navigate to the sub-path if specified
        if (!string.IsNullOrWhiteSpace(path))
        {
            var trimmedPath = path.Trim('/');
            var entry = tree[trimmedPath];

            if (entry?.Target is not Tree subTree)
                return Task.FromResult<IReadOnlyList<GitFileEntry>>([]);

            tree = subTree;
        }

        var entries = tree.Select(e => new GitFileEntry(
            Name: e.Name,
            Path: e.Path,
            Type: e.TargetType == TreeEntryTargetType.Tree ? "tree" : "blob",
            Size: e.TargetType == TreeEntryTargetType.Blob ? ((Blob)e.Target).Size : 0
        )).ToList();

        // Sort: directories first, then files, alphabetically within each group
        entries.Sort((a, b) =>
        {
            if (a.Type != b.Type)
                return a.Type == "tree" ? -1 : 1;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        return Task.FromResult<IReadOnlyList<GitFileEntry>>(entries);
    }

    /// <summary>
    /// Returns the content of a single file at the given ref and path.
    /// Returns null if the file doesn't exist.
    /// For binary files, returns base64-encoded content.
    /// </summary>
    public Task<GitFileContent?> GetFileContentAsync(
        string repoPath, string reference, string filePath, CancellationToken ct = default)
    {
        using var repo = new Repository(repoPath);

        var commit = ResolveCommit(repo, reference);
        if (commit is null)
            return Task.FromResult<GitFileContent?>(null);

        var entry = commit.Tree[filePath.Trim('/')];
        if (entry?.Target is not Blob blob)
            return Task.FromResult<GitFileContent?>(null);

        // Determine if the content is binary by checking for null bytes
        // in the first 8KB (same heuristic Git uses)
        using var stream = blob.GetContentStream();
        var buffer = new byte[Math.Min(blob.Size, 8192)];
        var bytesRead = stream.Read(buffer, 0, buffer.Length);
        var isBinary = buffer.AsSpan(0, bytesRead).Contains((byte)0);

        string content;
        string encoding;

        if (isBinary)
        {
            // Re-read the full content for base64
            using var fullStream = blob.GetContentStream();
            using var ms = new MemoryStream();
            fullStream.CopyTo(ms);
            content = Convert.ToBase64String(ms.ToArray());
            encoding = "base64";
        }
        else
        {
            using var reader = new StreamReader(blob.GetContentStream());
            content = reader.ReadToEnd();
            encoding = "utf-8";
        }

        return Task.FromResult<GitFileContent?>(new GitFileContent(
            Path: filePath,
            Content: content,
            Size: blob.Size,
            Encoding: encoding
        ));
    }

    /// <summary>
    /// Returns the commit log for a given reference, newest first.
    /// </summary>
    public Task<IReadOnlyList<GitCommitInfo>> GetCommitLogAsync(
        string repoPath, string reference, int maxCount = 20, CancellationToken ct = default)
    {
        using var repo = new Repository(repoPath);

        var commit = ResolveCommit(repo, reference);
        if (commit is null)
            return Task.FromResult<IReadOnlyList<GitCommitInfo>>([]);

        // Walk the commit graph from the resolved commit
        var commits = new List<GitCommitInfo>();
        var filter = new CommitFilter
        {
            IncludeReachableFrom = commit,
            SortBy = CommitSortStrategies.Time | CommitSortStrategies.Topological
        };

        foreach (var c in repo.Commits.QueryBy(filter).Take(maxCount))
        {
            commits.Add(new GitCommitInfo(
                Sha: c.Sha,
                Message: c.MessageShort,
                AuthorName: c.Author.Name,
                AuthorEmail: c.Author.Email,
                Timestamp: c.Author.When
            ));
        }

        return Task.FromResult<IReadOnlyList<GitCommitInfo>>(commits);
    }

    /// <summary>
    /// Returns the full path to a bare repository on disk.
    /// Used by the Git HTTP middleware to locate repos.
    /// </summary>
    public string GetRepositoryPath(Guid projectId) =>
        Path.Combine(_basePath, $"{projectId}.git");

    /// <summary>
    /// Resolves a reference string (branch name, tag, or SHA) to a Commit.
    /// Tries in order: exact branch, exact tag, SHA prefix lookup.
    /// Returns null if nothing matches.
    /// </summary>
    private static Commit? ResolveCommit(Repository repo, string reference)
    {
        // Try as a branch
        var branch = repo.Branches[reference];
        if (branch is not null)
            return branch.Tip;

        // Try as a tag
        var tag = repo.Tags[reference];
        if (tag?.Target is Commit tagCommit)
            return tagCommit;

        // Try as a SHA (full or prefix)
        try
        {
            var obj = repo.Lookup(reference);
            if (obj is Commit shaCommit)
                return shaCommit;
        }
        catch
        {
            // Not a valid object reference — ignore
        }

        // Fallback: HEAD
        return repo.Head?.Tip;
    }
}
