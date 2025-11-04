namespace RestaurantSystem.Api.Features.Settings.Dtos;

public class TaxConfigurationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsEnabled { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class CreateTaxConfigurationDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsEnabled { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class UpdateTaxConfigurationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsEnabled { get; set; }
    public string Description { get; set; } = string.Empty;
}
