namespace RestaurantSystem.Api.Settings;

public class FileStorageSettings
{
    public const string SectionName = "FileStorage";

    public string Provider { get; set; } = "Local"; // "S3", "Azure", "Local"
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB
    public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    public string[] AllowedMimeTypes { get; set; } = { "image/jpeg", "image/png", "image/gif", "image/webp" };
}
