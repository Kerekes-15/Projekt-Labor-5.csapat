using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using VTrailer.Models;
using VTrailer.Services;

namespace VTrailer.Presentation;

public sealed partial class BookingPage : Page
{
    private DatabaseService _dbService;
    private List<Trailer> _availableTrailers = new();
    private decimal _currentCalculatedPrice = 0;

    public BookingPage()
    {
        this.InitializeComponent();
        _dbService = new DatabaseService();
        LoadAvailableTrailers();
    }

    // 1. Lépés: Betöltjük a felhőből CSAK az "Elérhető" utánfutókat
    private async void LoadAvailableTrailers()
    {
        var allTrailers = await _dbService.GetTrailersAsync();

        // Kiszűrjük, hogy csak a szabadokat lehessen lefoglalni
        _availableTrailers = allTrailers.Where(t => t.Status == "Elérhető").ToList();

        // Betöltjük a legördülő menübe, és megmondjuk neki, hogy a nevet (BrandAndModel) mutassa
        TrailerComboBox.ItemsSource = _availableTrailers;
        TrailerComboBox.DisplayMemberPath = "BrandAndModel";
    }

    // 2. Lépés: Automatikus árszámolás, ha a felhasználó kattintgat a menükben
    private void OnSelectionChanged(object sender, object e)
    {
        if (TrailerComboBox.SelectedItem is Trailer selectedTrailer && TimeSlotComboBox.SelectedItem is string selectedTimeSlot)
        {
            // Ha délelőtt vagy délután, akkor az ár a fele. Ha egész nap, akkor a teljes napi díj.
            if (selectedTimeSlot.Contains("Egész nap"))
            {
                _currentCalculatedPrice = selectedTrailer.DailyRateFt;
            }
            else
            {
                _currentCalculatedPrice = selectedTrailer.DailyRateFt / 2;
            }

            TotalPriceText.Text = $"{_currentCalculatedPrice:N0} Ft";
        }
    }

    // 3. Lépés: A Gombnyomás! Mentés a felhőbe.
    private async void OnSubmitBookingClicked(object sender, RoutedEventArgs e)
    {
        // 1. Ellenőrzés: Minden ki van töltve?
        if (TrailerComboBox.SelectedItem is not Trailer selectedTrailer ||
            !BookingDatePicker.Date.HasValue ||
            TimeSlotComboBox.SelectedItem is not string selectedTimeSlot)
        {
            ShowMessage("Kérlek, tölts ki minden mezőt a foglaláshoz!", false);
            return;
        }

        // 2. Ellenőrzés: Be van jelentkezve valaki?
        var currentUser = _dbService.CurrentUser;
        if (currentUser == null)
        {
            // Mivel tesztelünk, ideiglenesen csinálunk egy "kamu" usert, ha épp nincs bejelentkezve
            currentUser = new User { Username = "tesztuser", FullName = "Teszt Elek" };
        }

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
        };

        try
        {
            SubmitButton.IsEnabled = false; // Kikapcsoljuk a gombot, amíg tölt

            // 4. Elküldjük a Supabase-nek a foglalást
            await _dbService.AddBookingAsync(newBooking);

            // 5. Átírjuk a lefoglalt utánfutó státuszát "Kölcsönözve" értékre
            await _dbService.UpdateTrailerStatusAsync(selectedTrailer.Id, "Kölcsönözve");

            ShowMessage("Sikeres foglalás! Az utánfutó állapota frissítve lett.", true);

            // Frissítjük a listát (hogy eltűnjön a most lefoglalt)
            TrailerComboBox.SelectedItem = null;
            LoadAvailableTrailers();
        }
        catch (Exception ex)
        {
            ShowMessage($"Hiba történt: {ex.Message}", false);
        }
        finally
        {
            SubmitButton.IsEnabled = true;
        }
    }

    // Segédfüggvény az üzenetek kiírásához
    private void ShowMessage(string message, bool isSuccess)
    {
        StatusMessage.Text = message;
        StatusMessage.Foreground = isSuccess ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green)
                                             : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        StatusMessage.Visibility = Visibility.Visible;
    }
}
