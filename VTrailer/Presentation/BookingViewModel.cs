using System.Collections.ObjectModel;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using VTrailer.Services;

namespace VTrailer.Presentation;

public partial class BookingViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly DeliveryQuoteService _deliveryQuoteService;

    public ObservableCollection<Trailer> AvailableTrailers { get; } = new();

    public ObservableCollection<string> TimeSlots { get; } =
    [
        "Délelőtt (08:00 - 12:00)",
        "Délután (13:00 - 17:00)",
        "Egész nap (08:00 - 17:00)"
    ];

    public BookingViewModel(
        DatabaseService databaseService,
        DeliveryQuoteService deliveryQuoteService,
        IOptions<DeliveryOptions> deliveryOptions)
    {
        _databaseService = databaseService;
        _deliveryQuoteService = deliveryQuoteService;
        PricingPerKilometerFt = deliveryOptions.Value.FeePerKilometerFt;

        PickupSelected = true;
        DeliverySelected = false;
        SelectedLocationLabel = "Még nincs kiválasztott kiszállítási hely.";
        DeliveryDistanceText = "0.0 km";
        DeliveryFeeText = "0 Ft";
        DeliveryStatusMessage = "A telephelyi átvétel díjmentes. Kiszállításhoz válaszd a Delivery opciót.";

        _ = LoadAvailableTrailersAsync();
        UpdateDerivedState();
    }

    [ObservableProperty]
    private Trailer? _selectedTrailer;

    [ObservableProperty]
    private DateTimeOffset? _selectedBookingDate;

    [ObservableProperty]
    private string? _selectedTimeSlot;

    [ObservableProperty]
    private bool _deliverySelected;

    [ObservableProperty]
    private bool _pickupSelected;

    [ObservableProperty]
    private bool _isCalculatingRoute;

    [ObservableProperty]
    private bool _isSubmitting;

    [ObservableProperty]
    private bool _isLoadingTrailers;

    [ObservableProperty]
    private string _selectedLocationLabel = string.Empty;

    [ObservableProperty]
    private string _selectedCoordinates = string.Empty;

    [ObservableProperty]
    private string _deliveryDistanceText = "0.0 km";

    [ObservableProperty]
    private string _deliveryFeeText = "0 Ft";

    [ObservableProperty]
    private string _deliveryStatusMessage = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isStatusSuccess;

    public decimal PricingPerKilometerFt { get; }

    public bool HasSelectedLocation => SelectedLongitude.HasValue && SelectedLatitude.HasValue;

    public bool HasDeliveryQuote => CurrentQuote is not null;

    public bool HasBookingBasics =>
        SelectedTrailer is not null &&
        SelectedBookingDate.HasValue &&
        !string.IsNullOrWhiteSpace(SelectedTimeSlot);

    public bool CanSubmit =>
        HasBookingBasics &&
        !IsSubmitting &&
        !IsLoadingTrailers &&
        (PickupSelected || (DeliverySelected && HasDeliveryQuote && !IsCalculatingRoute));

    public Visibility MapVisibility => DeliverySelected ? Visibility.Visible : Visibility.Collapsed;

    public Visibility PickupHintVisibility => PickupSelected ? Visibility.Visible : Visibility.Collapsed;

    public Visibility DeliveryValidationVisibility =>
        DeliverySelected && !HasDeliveryQuote && !IsCalculatingRoute ? Visibility.Visible : Visibility.Collapsed;

    public Visibility StatusVisibility =>
        string.IsNullOrWhiteSpace(StatusMessage) ? Visibility.Collapsed : Visibility.Visible;

    public string BasePriceText => $"{BaseRentalPriceFt:N0} Ft";

    public string TotalPriceText => $"{CalculatedTotalPriceFt:N0} Ft";

    public string RentalPriceSummaryText => $"Bérleti díj: {BasePriceText}";

    public string TotalPriceSummaryText => $"Fizetendő végösszeg: {TotalPriceText}";

    public string DistanceSummaryText => $"Szállítási útvonal: {DeliveryDistanceText}";

    public string FeeSummaryText => $"Extra díj: {DeliveryFeeText}";

    public Brush StatusForeground => IsStatusSuccess
        ? new SolidColorBrush(Microsoft.UI.Colors.ForestGreen)
        : new SolidColorBrush(Microsoft.UI.Colors.IndianRed);

    public double? SelectedLongitude { get; private set; }

    public double? SelectedLatitude { get; private set; }

    private DeliveryQuote? CurrentQuote { get; set; }

    private decimal BaseRentalPriceFt => CalculateBaseRentalPrice();

    private decimal CalculatedTotalPriceFt => BaseRentalPriceFt + (DeliverySelected ? CurrentQuote?.FeeFt ?? 0m : 0m);

    partial void OnSelectedTrailerChanged(Trailer? value)
    {
        ClearStatus();
        UpdateDerivedState();
    }

    partial void OnSelectedBookingDateChanged(DateTimeOffset? value)
    {
        ClearStatus();
        UpdateDerivedState();
    }

    partial void OnSelectedTimeSlotChanged(string? value)
    {
        ClearStatus();
        UpdateDerivedState();
    }

    public void SetTransportMode(bool isDelivery)
    {
        DeliverySelected = isDelivery;
        PickupSelected = !isDelivery;
        ClearStatus();

        if (!isDelivery)
        {
            CurrentQuote = null;
            SelectedLongitude = null;
            SelectedLatitude = null;
            SelectedLocationLabel = "Még nincs kiválasztott kiszállítási hely.";
            SelectedCoordinates = string.Empty;
            DeliveryDistanceText = "0.0 km";
            DeliveryFeeText = "0 Ft";
            DeliveryStatusMessage = "A telephelyi átvétel díjmentes. A térkép rejtve marad, és azonnal véglegesítheted a foglalást.";
        }
        else if (!HasSelectedLocation)
        {
            DeliveryStatusMessage = "Válassz egy pontot a térképen a kiszállítási díj kiszámításához.";
        }
        else if (CurrentQuote is not null)
        {
            DeliveryStatusMessage = $"A kiszállítási útvonal kiszámítva, díj: {DeliveryFeeText}.";
        }

        UpdateDerivedState();
    }

    public async Task SetSelectedLocationAsync(double longitude, double latitude, string? label, CancellationToken cancellationToken = default)
    {
        SelectedLongitude = longitude;
        SelectedLatitude = latitude;
        SelectedLocationLabel = string.IsNullOrWhiteSpace(label)
            ? $"Kiválasztott pont: {latitude:0.00000}, {longitude:0.00000}"
            : label;
        SelectedCoordinates = $"Lon {longitude:0.00000}, Lat {latitude:0.00000}";
        ClearStatus();

        await RefreshDeliveryQuoteAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task SubmitBookingAsync()
    {
        if (!CanSubmit || SelectedTrailer is null || !SelectedBookingDate.HasValue || string.IsNullOrWhiteSpace(SelectedTimeSlot))
        {
            SetStatus("Kérlek, tölts ki minden kötelező mezőt. Delivery esetén térképes helymegjelölés is szükséges.", false);
            return;
        }

        var currentUser = DatabaseService.CurrentUser;
        

        var newBooking = new Booking
        {
            TrailerId = SelectedTrailer.Id,
            TrailerName = SelectedTrailer.BrandAndModel,
            Email = currentUser!.Email,
            CustomerName = currentUser!.FullName,
            BookingDate = SelectedBookingDate.Value.Date,
            TimeSlot = SelectedTimeSlot,
            TotalPrice = CalculatedTotalPriceFt
        };

        try
        {
            IsSubmitting = true;
            UpdateDerivedState();

            await _databaseService.AddBookingAsync(newBooking);
            await _databaseService.UpdateTrailerStatusAsync(SelectedTrailer.Id, "Kölcsönözve");

            var successMessage = PickupSelected
                ? $"Sikeres foglalás. Fizetendő végösszeg: {TotalPriceText}. Telephelyi átvétel lett kiválasztva."
                : $"Sikeres foglalás. Fizetendő végösszeg: {TotalPriceText}. Kiszállítás ide: {SelectedLocationLabel}.";

            SetStatus(successMessage, true);
            await LoadAvailableTrailersAsync();
            ResetForm();
        }
        catch (Exception ex)
        {
            SetStatus($"Hiba történt a foglalás mentésekor: {ex.Message}", false);
        }
        finally
        {
            IsSubmitting = false;
            UpdateDerivedState();
        }
    }

    private async Task LoadAvailableTrailersAsync()
    {
        try
        {
            IsLoadingTrailers = true;
            UpdateDerivedState();

            var allTrailers = await _databaseService.GetTrailersAsync();
            var available = allTrailers
                .Where(t => string.Equals(t.Status, "Elérhető", StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.BrandAndModel)
                .ToList();

            AvailableTrailers.Clear();
            foreach (var trailer in available)
            {
                AvailableTrailers.Add(trailer);
            }

            if (SelectedTrailer is not null && AvailableTrailers.All(t => t.Id != SelectedTrailer.Id))
            {
                SelectedTrailer = null;
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Nem sikerült betölteni az utánfutókat: {ex.Message}", false);
        }
        finally
        {
            IsLoadingTrailers = false;
            UpdateDerivedState();
        }
    }

    private async Task RefreshDeliveryQuoteAsync(CancellationToken cancellationToken)
    {
        CurrentQuote = null;
        DeliveryDistanceText = "Számítás...";
        DeliveryFeeText = "Számítás...";
        DeliveryStatusMessage = "Útvonal számítása az OpenRouteService használatával...";
        IsCalculatingRoute = true;
        UpdateDerivedState();

        try
        {
            var quote = await _deliveryQuoteService.GetQuoteAsync(
                SelectedLongitude ?? DeliveryOptions.DepotLongitude,
                SelectedLatitude ?? DeliveryOptions.DepotLatitude,
                cancellationToken);

            CurrentQuote = quote;
            DeliveryDistanceText = $"{quote.DistanceKilometers:0.0} km";
            DeliveryFeeText = $"{quote.FeeFt:N0} Ft";
            DeliveryStatusMessage = $"Kiszállítási díj kiszámítva {PricingPerKilometerFt:N0} Ft/km díjszabással.";
        }
        catch (Exception ex)
        {
            CurrentQuote = null;
            DeliveryDistanceText = "Nem elérhető";
            DeliveryFeeText = "Nem elérhető";
            DeliveryStatusMessage = ex.Message;
        }
        finally
        {
            IsCalculatingRoute = false;
            UpdateDerivedState();
        }
    }

    private decimal CalculateBaseRentalPrice()
    {
        if (SelectedTrailer is null || string.IsNullOrWhiteSpace(SelectedTimeSlot))
        {
            return 0m;
        }

        return SelectedTimeSlot.Contains("Egész nap", StringComparison.OrdinalIgnoreCase)
            ? SelectedTrailer.DailyRateFt
            : SelectedTrailer.DailyRateFt / 2m;
    }

    private void ResetForm()
    {
        SelectedTrailer = null;
        SelectedBookingDate = null;
        SelectedTimeSlot = null;
        SelectedLongitude = null;
        SelectedLatitude = null;
        SelectedLocationLabel = "Még nincs kiválasztott kiszállítási hely.";
        SelectedCoordinates = string.Empty;
        DeliveryDistanceText = "0.0 km";
        DeliveryFeeText = "0 Ft";
        CurrentQuote = null;
        DeliverySelected = false;
        PickupSelected = true;
        DeliveryStatusMessage = "A telephelyi átvétel díjmentes. Kiszállításhoz válaszd a Delivery opciót.";
        UpdateDerivedState();
    }

    private void SetStatus(string message, bool isSuccess)
    {
        StatusMessage = message;
        IsStatusSuccess = isSuccess;
        OnPropertyChanged(nameof(StatusVisibility));
        OnPropertyChanged(nameof(StatusForeground));
    }

    private void ClearStatus()
    {
        if (!string.IsNullOrWhiteSpace(StatusMessage))
        {
            StatusMessage = string.Empty;
            OnPropertyChanged(nameof(StatusVisibility));
        }
    }

    private void UpdateDerivedState()
    {
        OnPropertyChanged(nameof(HasSelectedLocation));
        OnPropertyChanged(nameof(HasDeliveryQuote));
        OnPropertyChanged(nameof(HasBookingBasics));
        OnPropertyChanged(nameof(CanSubmit));
        OnPropertyChanged(nameof(MapVisibility));
        OnPropertyChanged(nameof(PickupHintVisibility));
        OnPropertyChanged(nameof(DeliveryValidationVisibility));
        OnPropertyChanged(nameof(StatusVisibility));
        OnPropertyChanged(nameof(BasePriceText));
        OnPropertyChanged(nameof(TotalPriceText));
        OnPropertyChanged(nameof(RentalPriceSummaryText));
        OnPropertyChanged(nameof(TotalPriceSummaryText));
        OnPropertyChanged(nameof(DistanceSummaryText));
        OnPropertyChanged(nameof(FeeSummaryText));
        OnPropertyChanged(nameof(StatusForeground));
    }
}
