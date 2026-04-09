using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using GitLabClone.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GitLabClone.Infrastructure.Storage;

public sealed class AzureBlobStorageService(
    IConfiguration configuration,
    ILogger<AzureBlobStorageService> logger
) : IBlobStorageService
{
    private readonly Lazy<BlobServiceClient> _client = new(() =>
    {
        var connectionString = configuration["AzureBlob:ConnectionString"]
            ?? throw new InvalidOperationException("AzureBlob:ConnectionString not configured.");
        return new BlobServiceClient(connectionString);
    });

    public async Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType, CancellationToken ct = default)
    {
        var containerClient = _client.Value.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        logger.LogInformation("Uploaded blob {BlobName} to {Container}", blobName, containerName);
        return blobClient.Uri.ToString();
    }

    public async Task<Stream?> DownloadAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        var containerClient = _client.Value.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(ct))
            return null;

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        var containerClient = _client.Value.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }

    public Task<string> GetBlobUrlAsync(string containerName, string blobName)
    {
        var containerClient = _client.Value.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        return Task.FromResult(blobClient.Uri.ToString());
    }
}
