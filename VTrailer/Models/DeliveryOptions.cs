namespace VTrailer.Models;

public sealed class DeliveryOptions
{
    public const double DepotLongitude = 19.19105;
    public const double DepotLatitude = 47.50343;

    public string? OpenRouteServiceApiKey { get; init; }

    public decimal FeePerKilometerFt { get; init; } = 250m;
}
