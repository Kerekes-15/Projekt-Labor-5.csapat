using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VTrailer.Models;
using VTrailer.Services;

namespace VTrailer.Presentation;

public sealed partial class AllBookingsPage : Page
{
    private DatabaseService _dbService = new DatabaseService();

    public AllBookingsPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var user = DatabaseService.CurrentUser;

        if (user == null || (user.Role != "Adminisztrátor" && user.Role != "Alkalmazott"))
        {
            Frame.Navigate(typeof(HomePage));
            return;
        }

        await RefreshData();
    }

    private async Task RefreshData()
    {
        var bookings = await _dbService.GetAllBookingsAsync();
        var displayItems = BuildAllBookingDisplayItems(bookings);

        if (displayItems != null && displayItems.Any())
        {
            AllBookingsListView.ItemsSource = displayItems;
            EmptyStateText.Visibility = Visibility.Collapsed;
            AllBookingsListView.Visibility = Visibility.Visible;
        }
        else
        {
            AllBookingsListView.Visibility = Visibility.Collapsed;
            EmptyStateText.Visibility = Visibility.Visible;
        }
    }

    private List<BookingDisplayItem> BuildAllBookingDisplayItems(IEnumerable<Booking>? bookings)
    {
        if (bookings is null) return new List<BookingDisplayItem>();

        var items = new List<BookingDisplayItem>();
        var multiDayGroups = new Dictionary<string, List<Booking>>();

        foreach (var booking in bookings.OrderByDescending(b => b.BookingDate))
        {
          
            if (BookingTimeSlotMetadata.TryParseMultiDay(booking.TimeSlot, out var startDate, out var endDate))
            {
           
                var key = $"{booking.CustomerName}:{booking.TrailerId}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
                if (!multiDayGroups.TryGetValue(key, out var group))
                {
                    group = new List<Booking>();
                    multiDayGroups[key] = group;
                }
                group.Add(booking);
                continue;
            }


            items.Add(new BookingDisplayItem(booking));
        }


        foreach (var group in multiDayGroups.Values)
        {
            var first = group[0];
            BookingTimeSlotMetadata.TryParseMultiDay(first.TimeSlot, out var start, out var end);
            var dayCount = (end - start).Days + 1;
            var totalPrice = group.Sum(b => b.TotalPrice);

            items.Add(new BookingDisplayItem(
                first,
                $"{start:yyyy. MM. dd.} - {end:yyyy. MM. dd.}",
                $"Egész nap ({dayCount} nap)",
                totalPrice));
        }

        return items.OrderByDescending(i => i.SortDate).ToList();
    }

    private async void OnReturnTrailerClick(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is BookingDisplayItem selectedItem)
        {
            var selectedBooking = selectedItem.OriginalBooking;

            ContentDialog returnDialog = new ContentDialog
            {
                Title = "Visszavétel megerősítése",
                Content = $"Megerősíted, hogy a(z) {selectedBooking.CustomerName} által lefoglalt {selectedBooking.TrailerName} visszakerült a telephelyre?",
                PrimaryButtonText = "Igen, visszavéve",
                CloseButtonText = "Mégse",
                XamlRoot = this.XamlRoot
            };

            if (await returnDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                bool success = await _dbService.ReturnTrailerAsync(selectedBooking);
                if (success)
                {
                    await RefreshData();
                }
            }
        }
    }

    public class BookingDisplayItem
    {
        public Booking OriginalBooking { get; }
        public string TrailerName => OriginalBooking.TrailerName ?? "";
        public string CustomerName => OriginalBooking.CustomerName ?? "";
        public string DisplayDate { get; }
        public string DisplayTimeSlot { get; }
        public string DisplayPrice { get; }
        public DateTime SortDate { get; }

        public BookingDisplayItem(Booking b, string? customDate = null, string? customSlot = null, decimal? customPrice = null)
        {
            OriginalBooking = b;
            DisplayDate = customDate ?? b.BookingDate.ToString("yyyy. MM. dd.");
            DisplayTimeSlot = customSlot ?? b.TimeSlot ?? "";
            DisplayPrice = $"{(customPrice ?? b.TotalPrice):N0} Ft";
            SortDate = b.BookingDate;
        }
    }
}
