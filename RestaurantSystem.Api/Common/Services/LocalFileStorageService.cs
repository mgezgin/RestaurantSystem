using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;

namespace RestaurantSystem.Api.Common.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalFileStorageService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _basePath = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads");
        _baseUrl = configuration["LocalStorage:BaseUrl"]!; // e.g., "https://localhost:5001/uploads"

        // Ensure directory exists
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folder, string? fileName = null, CancellationToken cancellationToken = default)
    {
        fileName ??= GenerateUniqueFileName(file.FileName);
        var folderPath = Path.Combine(_basePath, folder);
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        return $"{_baseUrl.TrimEnd('/')}/{folder}/{fileName}";
    }

    public async Task<string> UploadFileAsync(Stream stream, string folder, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(_basePath, folder);
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);

        using var fileStream = new FileStream(filePath, FileMode.Create);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return $"{_baseUrl.TrimEnd('/')}/{folder}/{fileName}";
    }

    public Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var relativePath = ExtractRelativePathFromUrl(fileUrl);
            var filePath = Path.Combine(_basePath, relativePath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<string> GetSignedUrlAsync(string fileKey, TimeSpan expirationTime, CancellationToken cancellationToken = default)
    {
        // For local storage, just return the regular URL
        return Task.FromResult($"{_baseUrl.TrimEnd('/')}/{fileKey}");
    }

    public Task<bool> FileExistsAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var relativePath = ExtractRelativePathFromUrl(fileUrl);
            var filePath = Path.Combine(_basePath, relativePath);
            return Task.FromResult(File.Exists(filePath));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<FileMetadata?> GetFileMetadataAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var relativePath = ExtractRelativePathFromUrl(fileUrl);
            var filePath = Path.Combine(_basePath, relativePath);

            if (!File.Exists(filePath))
                return Task.FromResult<FileMetadata?>(null);

            var fileInfo = new FileInfo(filePath);
            var metadata = new FileMetadata
            {
                FileName = fileInfo.Name,
                ContentType = GetContentType(fileInfo.Extension),
                Size = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime,
                Url = fileUrl,
                Metadata = new Dictionary<string, string>()
            };

            return Task.FromResult<FileMetadata?>(metadata);
        }
        catch
        {
            return Task.FromResult<FileMetadata?>(null);
        }
    }

    private string ExtractRelativePathFromUrl(string fileUrl)
    {
        var uri = new Uri(fileUrl);
        return uri.AbsolutePath.TrimStart('/').Replace("uploads/", "");
    }

    private static string GetContentType(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }

    private static string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var guid = Guid.NewGuid().ToString("N")[..8];
        return $"{timestamp}_{guid}{extension}";
    }
}
