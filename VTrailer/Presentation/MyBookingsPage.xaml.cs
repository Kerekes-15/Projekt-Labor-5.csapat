using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VTrailer.Models;
using VTrailer.Services;

namespace VTrailer.Presentation;

public sealed partial class MyBookingsPage : Page
{
    private readonly DatabaseService? _dbService;

    public MyBookingsPage()
    {
        this.InitializeComponent();
        _dbService = (Application.Current as App)?.Host?.Services.GetService<DatabaseService>();
        Loaded += MyBookingsPage_Loaded;
    }

    private void MyBookingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadMyBookings();
    }

    private async void LoadMyBookings()
    {
        if (_dbService is null)
        {
            EmptyStateText.Text = "Nem sikerült csatlakozni a foglalási szolgáltatáshoz.";
            EmptyStateText.Visibility = Visibility.Visible;
            BookingsListView.Visibility = Visibility.Collapsed;
            return;
        }

        var currentUser = DatabaseService.CurrentUser;
        var username = currentUser?.Email ?? "";

        try
        {
            var myBookings = await _dbService.GetMyBookingsAsync(username);
            var bookingItems = BuildBookingItems(myBookings);

            if (bookingItems.Count == 0)
            {
                EmptyStateText.Visibility = Visibility.Visible;
                BookingsListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyStateText.Visibility = Visibility.Collapsed;
                BookingsListView.Visibility = Visibility.Visible;
                BookingsListView.ItemsSource = bookingItems;
            }
        }
        catch (Exception ex)
        {
            EmptyStateText.Text = $"Hiba a foglalások betöltésekor: {ex.Message}";
            EmptyStateText.Visibility = Visibility.Visible;
            BookingsListView.Visibility = Visibility.Collapsed;
        }
    }

    private static List<BookingListItem> BuildBookingItems(IEnumerable<Booking>? bookings)
    {
        if (bookings is null)
        {
            return [];
        }

        var items = new List<BookingListItem>();
        var multiDayGroups = new Dictionary<string, List<Booking>>();

        foreach (var booking in bookings.OrderByDescending(b => b.BookingDate))
        {
            if (BookingTimeSlotMetadata.TryParseMultiDay(booking.TimeSlot, out var startDate, out var endDate))
            {
                var key = $"{booking.TrailerId}:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}";
                if (!multiDayGroups.TryGetValue(key, out var groupedBookings))
                {
                    groupedBookings = [];
                    multiDayGroups[key] = groupedBookings;
                }

                groupedBookings.Add(booking);
                continue;
            }

            items.Add(new BookingListItem(
                booking.Id,
                booking.TrailerName ?? string.Empty,
                booking.DisplayDate,
                booking.DisplayTimeSlot,
                booking.DisplayPrice,
                booking.BookingDate));
        }

        foreach (var group in multiDayGroups.Values)
        {
            var firstBooking = group[0];
            BookingTimeSlotMetadata.TryParseMultiDay(firstBooking.TimeSlot, out var startDate, out var endDate);
            var dayCount = (endDate - startDate).Days + 1;
            var totalPrice = group.Sum(booking => booking.TotalPrice);

            items.Add(new BookingListItem(
                firstBooking.Id,
                firstBooking.TrailerName ?? string.Empty,
                $"{startDate:yyyy. MM. dd.} - {endDate:yyyy. MM. dd.}",
                $"Egész nap ({dayCount} nap)",
                $"{totalPrice:N0} Ft",
                startDate));
        }

        return items
            .OrderByDescending(item => item.SortDate)
            .ToList();
    }

    private sealed record BookingListItem(
        int BookingId,
        string TrailerName,
        string DisplayDate,
        string DisplayTimeSlot,
        string DisplayPrice,
        DateTime SortDate);


    private async void OnDeleteBookingClick(object sender, RoutedEventArgs e)
    {

        if ((sender as Button)?.DataContext is BookingListItem selectedItem)
        {

            ContentDialog deleteDialog = new ContentDialog
            {
                Title = "Foglalás lemondása",
                Content = $"Biztosan törölni szeretnéd a(z) {selectedItem.TrailerName} utánfutóra szóló foglalásodat? Ez a művelet nem vonható vissza.",
                PrimaryButtonText = "Igen, törlöm",
                CloseButtonText = "Mégse",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            ContentDialogResult result = await deleteDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {

                    bool success = await _dbService!.DeleteBookingAsync(selectedItem.BookingId, selectedItem.TrailerName);

                    if (success)    
                    {
                        LoadMyBookings();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Hiba a törlés során: {ex.Message}");
                }
            }
        }
    }
}
