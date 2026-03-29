using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;

namespace VTrailer.Presentation;

public sealed partial class BookingPage : Page
{
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
        MapView.NavigateToString(BookingMapHtmlBuilder.Build(DeliveryOptions.DepotLongitude, DeliveryOptions.DepotLatitude));
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
