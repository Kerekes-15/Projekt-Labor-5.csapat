using System.Globalization;
using System.IO;
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

    public async Task<DeliveryQuote> GetQuoteAsync(double userLongitude, double userLatitude, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.OpenRouteServiceApiKey))
        {
            throw CreateUnavailableException();
        }

        var requestUri =
            "https://api.openrouteservice.org/v2/directions/driving-car" +
            $"?api_key={Uri.EscapeDataString(_options.OpenRouteServiceApiKey)}" +
            $"=&start={userLongitude.ToString("0.#####", CultureInfo.InvariantCulture)},{userLatitude.ToString("0.#####", CultureInfo.InvariantCulture)}" +
            $"&end={DeliveryOptions.DepotLongitude.ToString("0.#####", CultureInfo.InvariantCulture)},{DeliveryOptions.DepotLatitude.ToString("0.#####", CultureInfo.InvariantCulture)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("accept", "application/json, application/geo+json, application/gpx+xml, img/png; charset=utf-8");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        await WriteResponseLogAsync(requestUri, response, responseContent, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateUnavailableException();
        }

        var route = JsonSerializer.Deserialize<DirectionsResponse>(responseContent, SerializerOptions);
        var summary = route?.Features?.FirstOrDefault()?.Properties?.Summary;

        if (summary is null)
        {
            throw CreateUnavailableException();
        }

        var distanceKm = summary.Distance / 1000d;
        var feeFt = decimal.Round((decimal)distanceKm * _options.FeePerKilometerFt, 0, MidpointRounding.AwayFromZero);

        return new DeliveryQuote(summary.Distance, summary.Duration, feeFt);
    }

    private static InvalidOperationException CreateUnavailableException()
        => new("A kiszállítási díj most nem elérhető. Kérlek, próbáld újra később.");

    private static async Task WriteResponseLogAsync(
        string requestUri,
        HttpResponseMessage response,
        string responseContent,
        CancellationToken cancellationToken)
    {
        var rootFolder = FindProjectRoot();
        var logPath = Path.Combine(rootFolder, "ors-response-log.txt");
        var logContent =
            $"Timestamp: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}{Environment.NewLine}" +
            $"Request: {requestUri}{Environment.NewLine}" +
            $"Status: {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}" +
            $"Response:{Environment.NewLine}{responseContent}{Environment.NewLine}";

        await File.WriteAllTextAsync(logPath, logContent, cancellationToken);
    }

    private static string FindProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var hasProjectFile = directory.GetFiles("VTrailer.csproj").Length > 0;
            var hasAppSettings = directory.GetFiles("appsettings.json").Length > 0;
            if (hasProjectFile || hasAppSettings)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return AppContext.BaseDirectory;
    }

    private sealed class DirectionsResponse
    {
        public List<RouteFeature>? Features { get; init; }
    }

    private sealed class RouteFeature
    {
        public RouteProperties? Properties { get; init; }
    }

    private sealed class RouteProperties
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
