namespace EAM.Application.Interfaces.Infrastructures;

/// <summary>
/// Abstraction over the blob/object store. The presentation layer hands raw streams
/// (never ASP.NET types) so the Application layer stays framework-agnostic.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads an image to the given container and returns its public URL.
    /// Throws <c>AppException.BadRequest</c> when the content type or size is not allowed.
    /// </summary>
    Task<string> UploadImageAsync(
        Stream content,
        string fileName,
        string contentType,
        string container,
        CancellationToken ct = default);

    /// <summary>Uploads an avatar into the configured avatar container and returns its URL.</summary>
    Task<string> UploadAvatarAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a blob given its full URL. Safe to call with null/empty or a URL that
    /// doesn't belong to this account — it simply no-ops.
    /// </summary>
    Task DeleteByUrlAsync(string? blobUrl, CancellationToken ct = default);
}
