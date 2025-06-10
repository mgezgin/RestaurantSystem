namespace RestaurantSystem.Api.Settings;

public class AzureStorageSettings
{
    public const string SectionName = "Azure:Storage";

    public string ConnectionString { get; set; } = null!;
    public string ContainerName { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;
}
