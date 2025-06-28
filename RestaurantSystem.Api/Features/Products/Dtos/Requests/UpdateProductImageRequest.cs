namespace RestaurantSystem.Api.Features.Products.Dtos.Requests;

// Request DTO for image update
public record UpdateProductImageRequest
{
    public string? AltText { get; init; }
    public bool? IsPrimary { get; init; }
    public int? SortOrder { get; init; }
}
