using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using VTrailer.Services;

namespace VTrailer.Presentation;

public sealed partial class MyBookingsPage : Page
{
    private DatabaseService _dbService;

    public MyBookingsPage()
    {
        this.InitializeComponent();
        _dbService = new DatabaseService();
        LoadMyBookings();
    }

    private async void LoadMyBookings()
    {
        var currentUser = DatabaseService.CurrentUser;

        // SZIGORÚ ELLENŐRZÉS: Ha nincs bejelentkezve, azonnal megállítjuk a betöltést
        if (currentUser == null || string.IsNullOrEmpty(currentUser.Email))
        {
            EmptyStateText.Visibility = Visibility.Visible;
            BookingsListView.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            // Lekérdezzük az aktuális felhasználó e-mail címe alapján a saját foglalásait
            var myBookings = await _dbService.GetMyBookingsAsync(currentUser.Email);

            if (myBookings == null || myBookings.Count == 0)
            {
                EmptyStateText.Visibility = Visibility.Visible;
                BookingsListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyStateText.Visibility = Visibility.Collapsed;
                BookingsListView.Visibility = Visibility.Visible;
                BookingsListView.ItemsSource = myBookings;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Hiba a betöltéskor: {ex.Message}");
        }
    }
}
