using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EAM.Application.Common;
using EAM.Application.Interfaces.Infrastructures;
using EAM.Application.Options;

namespace EAM.Infrastructure.Storage;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStorageService"/> (backend-proxy
/// upload: the file flows through the API, the account key never leaves the server).
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobStorageOptions _options;
    private readonly ILogger<AzureBlobStorageService> _logger;

    // Built lazily so the app still boots when storage isn't configured yet; the clear
    // error only surfaces when an upload is actually attempted.
    private readonly Lazy<BlobServiceClient> _client;

    public AzureBlobStorageService(
        IOptions<BlobStorageOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new Lazy<BlobServiceClient>(() =>
        {
            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
                throw AppException.BadRequest(
                    "Azure Storage is not configured (Storage:ConnectionString is empty).",
                    "storage_not_configured");
            return new BlobServiceClient(_options.ConnectionString);
        });
    }

    public async Task<string> UploadImageAsync(
        Stream content, string fileName, string contentType, string container, CancellationToken ct = default)
    {
        Validate(content, contentType);

        var containerClient = _client.Value.GetBlobContainerClient(container);
        await EnsureContainerAsync(containerClient, ct);

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext))
            ext = ExtensionFor(contentType);

        // Random blob name avoids collisions and stops a client from guessing/overwriting others.
        var blobName = $"{Guid.NewGuid():N}{ext}";
        var blob = containerClient.GetBlobClient(blobName);

        if (content.CanSeek)
            content.Position = 0;

        await blob.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType,
                    CacheControl = "public, max-age=31536000"
                }
            },
            ct);

        return blob.Uri.ToString();
    }

    public Task<string> UploadAvatarAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
        => UploadImageAsync(content, fileName, contentType, _options.AvatarContainer, ct);

    public async Task DeleteByUrlAsync(string? blobUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(blobUrl)) return;
        if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri)) return;

        // Path shape: /{container}/{blobName...}
        var path = uri.AbsolutePath.TrimStart('/').Split('/', 2);
        if (path.Length < 2) return;

        var container = path[0];
        var blobName = Uri.UnescapeDataString(path[1]);

        try
        {
            var blob = _client.Value.GetBlobContainerClient(container).GetBlobClient(blobName);
            await blob.DeleteIfExistsAsync(cancellationToken: ct);
        }
        catch (RequestFailedException ex)
        {
            // Best-effort cleanup — a failed delete of an old image must not fail the request.
            _logger.LogWarning(ex, "Failed to delete blob {BlobUrl}", blobUrl);
        }
    }

    private void Validate(Stream content, string contentType)
    {
        if (content is null || (content.CanSeek && content.Length == 0))
            throw AppException.BadRequest("The uploaded file is empty.", "empty_file");

        if (string.IsNullOrWhiteSpace(contentType) ||
            !_options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw AppException.BadRequest(
                $"Unsupported image type '{contentType}'. Allowed: {string.Join(", ", _options.AllowedContentTypes)}.",
                "unsupported_file_type");
        }

        if (content.CanSeek && content.Length > _options.MaxFileSizeBytes)
        {
            var maxMb = _options.MaxFileSizeBytes / (1024d * 1024d);
            throw AppException.BadRequest(
                $"The image exceeds the maximum size of {maxMb:0.#} MB.",
                "file_too_large");
        }
    }

    private async Task EnsureContainerAsync(BlobContainerClient containerClient, CancellationToken ct)
    {
        try
        {
            // Prefer anonymous blob-level read so the returned URL is directly viewable.
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "PublicAccessNotPermitted")
        {
            // Storage account has anonymous access disabled (the secure default). Create the
            // container private instead; the URL then needs a read SAS or account-level access.
            _logger.LogWarning(
                "Anonymous blob access is disabled on the account; creating container '{Container}' as private.",
                containerClient.Name);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        }
    }

    private static string ExtensionFor(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        _ => string.Empty
    };
}
