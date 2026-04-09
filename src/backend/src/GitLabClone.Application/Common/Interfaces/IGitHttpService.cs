namespace GitLabClone.Application.Common.Interfaces;

/// <summary>
/// Handles the Git Smart HTTP protocol — the low-level pack negotiation
/// that powers `git clone`, `git fetch`, and `git push` over HTTP(S).
///
/// This is separate from IGitService (which handles higher-level operations
/// like file browsing) because the HTTP protocol requires streaming raw
/// binary data via git-upload-pack/git-receive-pack processes, which is
/// a fundamentally different concern.
/// </summary>
public interface IGitHttpService
{
    /// <summary>
    /// Handles GET /info/refs?service={serviceName}
    /// Returns the ref advertisement for git-upload-pack or git-receive-pack.
    /// </summary>
    Task<GitServiceResult> GetInfoRefsAsync(string repoPath, string serviceName, CancellationToken ct = default);

    /// <summary>
    /// Handles POST /git-upload-pack (clone/fetch)
    /// Processes the client's want/have negotiation and returns packfile data.
    /// </summary>
    Task<GitServiceResult> ExecuteUploadPackAsync(string repoPath, Stream requestBody, CancellationToken ct = default);

    /// <summary>
    /// Handles POST /git-receive-pack (push)
    /// Receives the client's packfile and updates refs.
    /// </summary>
    Task<GitServiceResult> ExecuteReceivePackAsync(string repoPath, Stream requestBody, CancellationToken ct = default);
}

public record GitServiceResult(
    Stream Content,
    string ContentType,
    int StatusCode = 200
);
