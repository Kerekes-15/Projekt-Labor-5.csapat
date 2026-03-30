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

    // 1. Lépés: Betöltjük a felhõbõl CSAK az "Elérhetõ" utánfutókat
    private async void LoadAvailableTrailers()
    {
        if (_mapInitialized)
        {
            return;
        }

        // Kiszûrjük, hogy csak a szabadokat lehessen lefoglalni
        _availableTrailers = allTrailers.Where(t => t.Status == "Elérhetõ").ToList();
        _availableTrailers = allTrailers.Where(t => t.Status == "Elérhetõ").ToList();
        // Betöltjük a legördülõ menübe, és megmondjuk neki, hogy a nevet (BrandAndModel) mutassa
        TrailerComboBox.ItemsSource = _availableTrailers;
        TrailerComboBox.DisplayMemberPath = "BrandAndModel";
        TrailerComboBox.ItemsSource = _availableTrailers;
        TrailerComboBox.DisplayMemberPath = "BrandAndModel";
    // 2. Lépés: Automatikus árszámolás, ha a felhasználó kattintgat a menükben
    private void OnSelectionChanged(object sender, object e)
    {
        if (TrailerComboBox.SelectedItem is Trailer selectedTrailer && TimeSlotComboBox.SelectedItem is string selectedTimeSlot)
        {
            // Ha délelõtt vagy délután, akkor az ár a fele. Ha egész nap, akkor a teljes napi díj.
            if (selectedTimeSlot.Contains("Egész nap"))
            {
                _currentCalculatedPrice = selectedTrailer.DailyRateFt;
            }
            else
            {
                _currentCalculatedPrice = selectedTrailer.DailyRateFt / 2;
            }
            else
            {
                _currentCalculatedPrice = selectedTrailer.DailyRateFt / 2;
            }

    private async void MapView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (ViewModel is null)
    // 3. Lépés: A Gombnyomás! Mentés a felhõbe.
    private async void OnSubmitBookingClicked(object sender, RoutedEventArgs e)
    {
        // 1. Ellenõrzés: Minden ki van töltve?
        if (TrailerComboBox.SelectedItem is not Trailer selectedTrailer ||
            !BookingDatePicker.Date.HasValue ||
            TimeSlotComboBox.SelectedItem is not string selectedTimeSlot)
    {
        // 1. Ellenõrzés: Minden ki van töltve?
        if (TrailerComboBox.SelectedItem is not Trailer selectedTrailer ||
            !BookingDatePicker.Date.HasValue ||
        // 2. Ellenõrzés: Be van jelentkezve valaki?
        var currentUser = _dbService.CurrentUser;
        if (currentUser == null)
        {
            // Mivel tesztelünk, ideiglenesen csinálunk egy "kamu" usert, ha épp nincs bejelentkezve
            currentUser = new User { Username = "tesztuser", FullName = "Teszt Elek" };
        }
        if (currentUser == null)
        // 3. A Foglalás (Booking) adatainak összeállítása
        var newBooking = new Booking
        {
            TrailerId = selectedTrailer.Id,
            TrailerName = selectedTrailer.BrandAndModel,
            Username = currentUser.Username,
            CustomerName = currentUser.FullName,
            BookingDate = BookingDatePicker.Date.Value.DateTime,
            TimeSlot = selectedTimeSlot,
            TotalPrice = _currentCalculatedPrice
            Username = currentUser.Username,
            CustomerName = currentUser.FullName,
            BookingDate = BookingDatePicker.Date.Value.DateTime,
            TimeSlot = selectedTimeSlot,
            SubmitButton.IsEnabled = false; // Kikapcsoljuk a gombot, amíg tölt

            // 4. Elküldjük a Supabase-nek a foglalást
            await _dbService.AddBookingAsync(newBooking);

            // 5. Átírjuk a lefoglalt utánfutó státuszát "Kölcsönözve" értékre
            await _dbService.UpdateTrailerStatusAsync(selectedTrailer.Id, "Kölcsönözve");

            ShowMessage("Sikeres foglalás! Az utánfutó állapota frissítve lett.", true);

            // Frissítjük a listát (hogy eltûnjön a most lefoglalt)
            TrailerComboBox.SelectedItem = null;
            LoadAvailableTrailers();
        }
        catch (Exception ex)
        {
            ShowMessage($"Hiba történt: {ex.Message}", false);
            ShowMessage("Sikeres foglalás! Az utánfutó állapota frissítve lett.", true);

            // Frissítjük a listát (hogy eltûnjön a most lefoglalt)
            TrailerComboBox.SelectedItem = null;
            LoadAvailableTrailers();
        }
        catch (Exception ex)
        {
            ShowMessage($"Hiba történt: {ex.Message}", false);
        }
    // Segédfüggvény az üzenetek kiírásához
    private void ShowMessage(string message, bool isSuccess)
        {
            var nestedJson = JsonSerializer.Deserialize<string>(json, options);
            return string.IsNullOrWhiteSpace(nestedJson)
                ? null
                : JsonSerializer.Deserialize<MapSelectionMessage>(nestedJson, options);
        }
    }

    // Segédfüggvény az üzenetek kiírásához
    private void ShowMessage(string message, bool isSuccess)
    {
        public string? Type { get; init; }

        public double? Latitude { get; init; }

        public double? Longitude { get; init; }

        public string? Label { get; init; }
    }
}
