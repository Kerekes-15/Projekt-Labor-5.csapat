using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace VTrailer.Services;

public sealed class DeliveryQuoteService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly DeliveryOptions _options;

    public DeliveryQuoteService(HttpClient httpClient, IOptions<DeliveryOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<DeliveryQuote> GetQuoteAsync(double destinationLongitude, double destinationLatitude, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.OpenRouteServiceApiKey))
        {
            throw new InvalidOperationException("Add your OpenRouteService API key in appsettings before using delivery quotes.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openrouteservice.org/v2/directions/driving-car");
        request.Headers.Add("Authorization", _options.OpenRouteServiceApiKey);
        request.Content = JsonContent.Create(new
        {
            coordinates = new[]
            {
                new[] { DeliveryOptions.DepotLongitude, DeliveryOptions.DepotLatitude },
                new[] { destinationLongitude, destinationLatitude }
            }
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenRouteService returned {(int)response.StatusCode}: {responseContent}");
        }

        var route = JsonSerializer.Deserialize<DirectionsResponse>(responseContent, SerializerOptions);
        var summary = route?.Routes?.FirstOrDefault()?.Summary;

        if (summary is null)
        {
            throw new InvalidOperationException("OpenRouteService did not return a usable route summary.");
        }

        var distanceKm = summary.Distance / 1000d;
        var feeFt = decimal.Round((decimal)distanceKm * _options.FeePerKilometerFt, 0, MidpointRounding.AwayFromZero);

        return new DeliveryQuote(summary.Distance, summary.Duration, feeFt);
    }

    private sealed class DirectionsResponse
    {
        public List<RouteResponse>? Routes { get; init; }
    }

    private sealed class RouteResponse
    {
        public RouteSummary? Summary { get; init; }
    }

    private sealed class RouteSummary
    {
        public double Distance { get; init; }

        public double Duration { get; init; }
    }
}

public sealed record DeliveryQuote(double DistanceMeters, double DurationSeconds, decimal FeeFt)
{
    public double DistanceKilometers => DistanceMeters / 1000d;

    public string DistanceText => $"{DistanceKilometers.ToString("0.0", CultureInfo.InvariantCulture)} km";
}
