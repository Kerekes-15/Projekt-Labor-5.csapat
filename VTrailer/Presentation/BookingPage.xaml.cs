using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;

namespace VTrailer.Presentation;

public sealed partial class BookingPage : Page
{
    private const string MapHostName = "maps.vtrailer.local";
    private const string MapFileName = "booking-map.html";
    private bool _mapInitialized;

    public BookingPage()
    {
        this.InitializeComponent();
        Loaded += BookingPage_Loaded;
    }

    private BookingViewModel? ViewModel => DataContext as BookingViewModel;

    private async void BookingPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_mapInitialized)
        {
            return;
        }

        _mapInitialized = true;

        await MapView.EnsureCoreWebView2Async();
        await InitializeMapAsync();
    }

    private async Task InitializeMapAsync()
    {
        if (MapView.CoreWebView2 is null)
        {
            return;
        }

        var mapHostFolder = Path.Combine(Path.GetTempPath(), "VTrailer", "MapHost");
        Directory.CreateDirectory(mapHostFolder);

        var mapFilePath = Path.Combine(mapHostFolder, MapFileName);
        var html = BookingMapHtmlBuilder.Build(DeliveryOptions.DepotLongitude, DeliveryOptions.DepotLatitude);
        await File.WriteAllTextAsync(mapFilePath, html);

        MapView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            MapHostName,
            mapHostFolder,
            CoreWebView2HostResourceAccessKind.Allow);

        MapView.CoreWebView2.Navigate($"https://{MapHostName}/{MapFileName}");
    }

    private void PickupOption_Checked(object sender, RoutedEventArgs e)
    {
        ViewModel?.SetTransportMode(isDelivery: false);
    }

    private void DeliveryOption_Checked(object sender, RoutedEventArgs e)
    {
        ViewModel?.SetTransportMode(isDelivery: true);
    }

    private async void MapView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (ViewModel is null)
        {
            return;
        }

        var message = ParseMessage(args.WebMessageAsJson);
        if (message?.Type != "locationSelected" || message.Longitude is null || message.Latitude is null)
        {
            return;
        }

        await ViewModel.SetSelectedLocationAsync(
            message.Longitude.Value,
            message.Latitude.Value,
            message.Label);
    }

    private static MapSelectionMessage? ParseMessage(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            return JsonSerializer.Deserialize<MapSelectionMessage>(json, options);
        }
        catch (JsonException)
        {
            var nestedJson = JsonSerializer.Deserialize<string>(json, options);
            return string.IsNullOrWhiteSpace(nestedJson)
                ? null
                : JsonSerializer.Deserialize<MapSelectionMessage>(nestedJson, options);
        }
    }

    private sealed class MapSelectionMessage
    {
        public string? Type { get; init; }

        public double? Latitude { get; init; }

        public double? Longitude { get; init; }

        public string? Label { get; init; }
    }
}
