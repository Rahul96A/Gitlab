using System.IO.Compression;
using GitLabClone.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitLabClone.Infrastructure.Git;

/// <summary>
/// Background service that synchronizes bare Git repositories to/from Azure Blob Storage.
///
/// Why this exists:
/// Azure Container Apps use ephemeral storage. If a container restarts, local files are lost.
/// This service ensures every repository is backed up to Azure Blob Storage as a compressed
/// tar archive. On startup, it restores any missing repos from Blob before the API starts
/// accepting Git traffic.
///
/// Sync strategy:
/// - BACKUP: After each push (receive-pack), the repo is tar.gz'd and uploaded to Blob.
///   A background queue debounces rapid pushes (waits 5 seconds after last push).
/// - RESTORE: On application startup, checks for repos in Blob that don't exist locally
///   and downloads + extracts them.
///
/// Container: "repositories"
/// Blob naming: "{projectId}.tar.gz"
///
/// Trade-offs:
/// - This is a pragmatic MVP approach. A production system might use Azure Files (NFS mount)
///   or a custom FUSE driver for transparent Blob-backed storage.
/// - There's a small window of data loss between the last push and the next sync.
///   For MVP, this is acceptable. Phase 6 adds a post-receive hook for immediate sync.
/// </summary>
public sealed class GitBlobSyncService(
    IBlobStorageService blobService,
    IConfiguration configuration,
    ILogger<GitBlobSyncService> logger
) : IGitBlobSyncService
{
    private const string ContainerName = "repositories";
    private readonly string _basePath = Path.GetFullPath(
        configuration["Git:RepoBasePath"]
        ?? Path.Combine(Path.GetTempPath(), "gitlabclone-repos")
    );

    /// <summary>
    /// Backs up a bare repository to Azure Blob Storage.
    /// Creates a tar.gz archive of the entire .git directory.
    /// </summary>
    public async Task BackupRepositoryAsync(Guid projectId, CancellationToken ct = default)
    {
        var repoPath = Path.Combine(_basePath, $"{projectId}.git");
        if (!Directory.Exists(repoPath))
        {
            logger.LogWarning("Cannot backup repo {ProjectId}: directory not found at {Path}", projectId, repoPath);
            return;
        }

        var blobName = $"{projectId}.tar.gz";
        var tempFile = Path.GetTempFileName();

        try
        {
            // Create tar.gz of the bare repo
            await using (var fileStream = File.Create(tempFile))
            await using (var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal))
            {
                await CreateTarAsync(repoPath, gzipStream, ct);
            }

            // Upload to Blob
            await using var uploadStream = File.OpenRead(tempFile);
            await blobService.UploadAsync(ContainerName, blobName, uploadStream, "application/gzip", ct);

            logger.LogInformation("Backed up repo {ProjectId} to Blob ({Size} bytes)", projectId, new FileInfo(tempFile).Length);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Restores a bare repository from Azure Blob Storage.
    /// Skips if the local directory already exists (no overwrite).
    /// </summary>
    public async Task RestoreRepositoryAsync(Guid projectId, CancellationToken ct = default)
    {
        var repoPath = Path.Combine(_basePath, $"{projectId}.git");
        if (Directory.Exists(repoPath))
        {
            logger.LogDebug("Repo {ProjectId} already exists locally, skipping restore", projectId);
            return;
        }

        var blobName = $"{projectId}.tar.gz";
        var stream = await blobService.DownloadAsync(ContainerName, blobName, ct);

        if (stream is null)
        {
            logger.LogWarning("No backup found in Blob for repo {ProjectId}", projectId);
            return;
        }

        var tempFile = Path.GetTempFileName();
        try
        {
            // Download to temp file
            await using (var fileStream = File.Create(tempFile))
            {
                await stream.CopyToAsync(fileStream, ct);
            }

            // Extract tar.gz
            Directory.CreateDirectory(repoPath);
            await using var fileStream2 = File.OpenRead(tempFile);
            await using var gzipStream = new GZipStream(fileStream2, CompressionMode.Decompress);
            await ExtractTarAsync(gzipStream, repoPath, ct);

            logger.LogInformation("Restored repo {ProjectId} from Blob", projectId);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Restores all repos for projects whose local directory is missing.
    /// Called on application startup.
    /// </summary>
    public async Task RestoreAllMissingAsync(IEnumerable<(Guid ProjectId, string RepoPath)> projects, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_basePath);

        foreach (var (projectId, repoPath) in projects)
        {
            if (!Directory.Exists(repoPath))
            {
                try
                {
                    await RestoreRepositoryAsync(projectId, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to restore repo {ProjectId} from Blob", projectId);
                }
            }
        }
    }

    /// <summary>
    /// Creates a simple tar archive from a directory.
    /// We use a minimal tar implementation because System.Formats.Tar may not
    /// be available on all target frameworks, and we only need basic archiving.
    /// </summary>
    private static async Task CreateTarAsync(string sourceDir, Stream output, CancellationToken ct)
    {
        // Use System.Formats.Tar which is available in .NET 7+
        await Task.Run(() =>
        {
            System.Formats.Tar.TarFile.CreateFromDirectory(
                sourceDir, output, includeBaseDirectory: false);
        }, ct);
    }

    private static async Task ExtractTarAsync(Stream input, string destinationDir, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            System.Formats.Tar.TarFile.ExtractToDirectory(input, destinationDir, overwriteFiles: false);
        }, ct);
    }
}

/// <summary>
/// Application-layer interface for the Blob sync service.
/// </summary>
public interface IGitBlobSyncService
{
    Task BackupRepositoryAsync(Guid projectId, CancellationToken ct = default);
    Task RestoreRepositoryAsync(Guid projectId, CancellationToken ct = default);
    Task RestoreAllMissingAsync(IEnumerable<(Guid ProjectId, string RepoPath)> projects, CancellationToken ct = default);
}
