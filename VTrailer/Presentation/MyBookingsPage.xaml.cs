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
        // Megnézzük, ki van bejelentkezve
        var currentUser = _dbService.CurrentUser;

        // Ha tesztelünk és nincs bejelentkezve senki, a "tesztuser" adatait kérjük le
        var username = currentUser?.Username ?? "tesztuser";

        try
        {
            // Lekérjük a felhőből az adatokat
            var myBookings = await _dbService.GetMyBookingsAsync(username);

            // Ha üres a lista, megmutatjuk az "üres" üzenetet
            if (myBookings == null || myBookings.Count == 0)
            {
                EmptyStateText.Visibility = Visibility.Visible;
                BookingsListView.Visibility = Visibility.Collapsed;
            }
            else // Ha vannak foglalások, betöltjük a listába
            {
                EmptyStateText.Visibility = Visibility.Collapsed;
                BookingsListView.Visibility = Visibility.Visible;
                BookingsListView.ItemsSource = myBookings;
            }
        }
        catch (Exception ex)
        {
            // Hiba esetén itt tudunk jelezni (most csendben elnyeljük)
            System.Diagnostics.Debug.WriteLine($"Hiba a betöltéskor: {ex.Message}");
        }
    }
}
