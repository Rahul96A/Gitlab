using System.Diagnostics;
using System.Text;
using GitLabClone.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace GitLabClone.Infrastructure.Git;

/// <summary>
/// Implements the Git Smart HTTP protocol by delegating to the `git` CLI.
///
/// Why CLI instead of LibGit2Sharp?
/// LibGit2Sharp doesn't support the pack protocol (upload-pack/receive-pack).
/// The pack protocol involves a complex multi-step negotiation between client
/// and server, and the only way to do it correctly is through the actual git
/// binary. This is what Gitea, GitBucket, Gogs, and even GitHub do.
///
/// The git binary is expected to be available in PATH (comes with the Docker
/// image or can be installed via `apt install git` on the container).
///
/// Security: Repository paths are validated before reaching this service.
/// The middleware ensures only authenticated users with proper permissions
/// can execute receive-pack (push). Upload-pack (clone/fetch) respects
/// project visibility settings.
/// </summary>
public sealed class GitHttpService(
    ILogger<GitHttpService> logger
) : IGitHttpService
{
    /// <summary>
    /// GET /info/refs?service=git-upload-pack (or git-receive-pack)
    ///
    /// Returns the "smart" ref advertisement. The response format is:
    /// - 4-byte pkt-line header "# service=git-upload-pack\n"
    /// - flush-pkt "0000"
    /// - Output of `git upload-pack --stateless-rpc --advertise-refs`
    ///
    /// This tells the client what refs (branches/tags) exist and what
    /// capabilities the server supports.
    /// </summary>
    public async Task<GitServiceResult> GetInfoRefsAsync(
        string repoPath, string serviceName, CancellationToken ct = default)
    {
        ValidateServiceName(serviceName);

        var output = new MemoryStream();

        // Write the service header pkt-line
        var header = $"# service={serviceName}\n";
        var pktHeader = $"{(header.Length + 4):x4}{header}";
        await output.WriteAsync(Encoding.ASCII.GetBytes(pktHeader), ct);
        await output.WriteAsync("0000"u8.ToArray(), ct);

        // Run git <service> --stateless-rpc --advertise-refs <repo>
        var gitService = serviceName.Replace("git-", "");
        var result = await RunGitAsync(
            $"{gitService} --stateless-rpc --advertise-refs \"{repoPath}\"",
            inputStream: null,
            ct
        );

        await result.CopyToAsync(output, ct);
        output.Position = 0;

        return new GitServiceResult(
            Content: output,
            ContentType: $"application/x-{serviceName}-advertisement"
        );
    }

    /// <summary>
    /// POST /git-upload-pack
    ///
    /// Client sends want/have lines, server responds with a packfile
    /// containing the requested objects. This powers `git clone` and `git fetch`.
    ///
    /// The --stateless-rpc flag makes git work in a single request/response
    /// cycle (instead of the bidirectional stream used in SSH transport).
    /// </summary>
    public async Task<GitServiceResult> ExecuteUploadPackAsync(
        string repoPath, Stream requestBody, CancellationToken ct = default)
    {
        logger.LogInformation("Executing git-upload-pack on {RepoPath}", repoPath);

        var result = await RunGitAsync(
            $"upload-pack --stateless-rpc \"{repoPath}\"",
            inputStream: requestBody,
            ct
        );

        return new GitServiceResult(
            Content: result,
            ContentType: "application/x-git-upload-pack-result"
        );
    }

    /// <summary>
    /// POST /git-receive-pack
    ///
    /// Client sends a packfile with new objects and ref update commands.
    /// Server applies the changes and responds with the result.
    /// This powers `git push`.
    /// </summary>
    public async Task<GitServiceResult> ExecuteReceivePackAsync(
        string repoPath, Stream requestBody, CancellationToken ct = default)
    {
        logger.LogInformation("Executing git-receive-pack on {RepoPath}", repoPath);

        var result = await RunGitAsync(
            $"receive-pack --stateless-rpc \"{repoPath}\"",
            inputStream: requestBody,
            ct
        );

        return new GitServiceResult(
            Content: result,
            ContentType: "application/x-git-receive-pack-result"
        );
    }

    private static void ValidateServiceName(string serviceName)
    {
        if (serviceName is not ("git-upload-pack" or "git-receive-pack"))
            throw new ArgumentException($"Invalid Git service: {serviceName}");
    }

    /// <summary>
    /// Runs a git command and returns its stdout as a stream.
    /// If inputStream is provided, it's piped to stdin (used for pack data).
    ///
    /// Timeout: 5 minutes — pack operations on large repos can be slow.
    /// </summary>
    private async Task<Stream> RunGitAsync(string arguments, Stream? inputStream, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardInput = inputStream is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start git process.");

        // Pipe request body to git stdin if provided
        if (inputStream is not null)
        {
            await inputStream.CopyToAsync(process.StandardInput.BaseStream, ct);
            process.StandardInput.Close();
        }

        // Read all stdout into memory — we need to return it as a seekable stream
        // because the middleware needs Content-Length for the HTTP response.
        var outputStream = new MemoryStream();
        await process.StandardOutput.BaseStream.CopyToAsync(outputStream, ct);

        // Read stderr for logging
        var stderr = await process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            logger.LogError("git {Arguments} failed (exit {ExitCode}): {StdErr}",
                arguments, process.ExitCode, stderr);
            throw new InvalidOperationException($"Git operation failed: {stderr}");
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            logger.LogDebug("git stderr: {StdErr}", stderr);
        }

        outputStream.Position = 0;
        return outputStream;
    }
}
