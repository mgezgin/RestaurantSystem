namespace RestaurantSystem.Api.Common.Models;

public class FileMetadata
{
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long Size { get; set; }
    public DateTime? LastModified { get; set; }
    public string Url { get; set; } = null!;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
