namespace EAM.Application.Options;

/// <summary>
/// Azure Blob Storage settings. The connection string is a secret and must be supplied
/// via environment variable (Storage__ConnectionString) or user-secrets, never committed.
/// </summary>
public class BlobStorageOptions
{
    public const string SectionName = "Storage";

    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Container for user avatar images.</summary>
    public string AvatarContainer { get; set; } = "avatars";

    /// <summary>Container for product/icon images.</summary>
    public string ProductContainer { get; set; } = "products";

    /// <summary>Maximum allowed upload size in bytes (default 5 MB).</summary>
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>Whitelisted image MIME types.</summary>
    public string[] AllowedContentTypes { get; set; } =
        { "image/jpeg", "image/png", "image/webp", "image/gif" };
}
