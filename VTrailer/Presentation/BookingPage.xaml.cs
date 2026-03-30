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

    private async void LoadAvailableTrailers()
    {
        var allTrailers = await _dbService.GetTrailersAsync();

        _availableTrailers = allTrailers.Where(t => t.Status == "Elérhető").ToList();

        TrailerComboBox.ItemsSource = _availableTrailers;
        TrailerComboBox.DisplayMemberPath = "BrandAndModel";
    }

    private void OnSelectionChanged(object sender, object e)
    {
        if (TrailerComboBox.SelectedItem is Trailer selectedTrailer && TimeSlotComboBox.SelectedItem is string selectedTimeSlot)
        {
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

    private async void OnSubmitBookingClicked(object sender, RoutedEventArgs e)
    {
        // 1. Ellenőrzés: Minden ki van töltve a felületen?
        if (TrailerComboBox.SelectedItem is not Trailer selectedTrailer ||
            !BookingDatePicker.Date.HasValue ||
            TimeSlotComboBox.SelectedItem is not string selectedTimeSlot)
        {
            ShowMessage("Kérlek, tölts ki minden mezőt a foglaláshoz!", false);
            return;
        }

        // 2. SZIGORÚ ELLENŐRZÉS: Csak valós, bejelentkezett felhasználó foglalhat
        var currentUser = DatabaseService.CurrentUser;
        if (currentUser == null)
        {
            ShowMessage("Hiba: A foglaláshoz be kell jelentkezned!", false);
            return;
        }

        // 3. A Foglalás (Booking) adatainak összeállítása
        var newBooking = new Booking
        {
            TrailerId = selectedTrailer.Id,
            TrailerName = selectedTrailer.BrandAndModel,
            Email = currentUser.Email,
            CustomerName = currentUser.FullName,
            BookingDate = BookingDatePicker.Date.Value.DateTime,
            TimeSlot = selectedTimeSlot,
            TotalPrice = _currentCalculatedPrice
        };

        try
        {
            SubmitButton.IsEnabled = false;

            await _dbService.AddBookingAsync(newBooking);
            await _dbService.UpdateTrailerStatusAsync(selectedTrailer.Id, "Kölcsönözve");

            ShowMessage("Sikeres foglalás! Az utánfutó állapota frissítve lett.", true);

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

    private void ShowMessage(string message, bool isSuccess)
    {
        StatusMessage.Text = message;
        StatusMessage.Foreground = isSuccess ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green)
                                             : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        StatusMessage.Visibility = Visibility.Visible;
    }
}
