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
                var key = $"{booking.Email}:{booking.TrailerId}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
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
                
                Content = $"Megerősíted, hogy a(z) {selectedItem.CustomerName} által lefoglalt {selectedItem.TrailerName} visszakerült a telephelyre?",
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

        public string TrailerName => OriginalBooking.TrailerName ?? "Ismeretlen utánfutó";

        public string CustomerName => OriginalBooking.CustomerName ?? "Ismeretlen felhasználó";

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
    private async void OnNewBookingClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ShowEmployeeBookingDialogAsync();
    }

    private async Task ShowEmployeeBookingDialogAsync()
    {
        // Dialógus inicializálása
        var dialog = new ContentDialog
        {
            Title = "Új foglalás rögzítése",
            PrimaryButtonText = "Mentés",
            CloseButtonText = "Mégse",
            XamlRoot = this.XamlRoot,
            IsPrimaryButtonEnabled = false
        };

        // Foglalt dátumok tárolója
        List<(DateTime Start, DateTime End)> bookedDates = new();

        // Beviteli mezők
        var nameBox = new TextBox { PlaceholderText = "pl. Kovács János", Height = 36, HorizontalAlignment = HorizontalAlignment.Stretch };
        var phoneBox = new TextBox { PlaceholderText = "pl. +36 30 123 4567", Height = 36, HorizontalAlignment = HorizontalAlignment.Stretch };
        var emailBox = new TextBox { PlaceholderText = "Email cím", Height = 36, HorizontalAlignment = HorizontalAlignment.Stretch };
        var trailerCombo = new ComboBox {Height = 36, HorizontalAlignment = HorizontalAlignment.Stretch, MaxDropDownHeight = 250 };

        var allTrailers = await _dbService.GetTrailersAsync();
        trailerCombo.ItemsSource = allTrailers.Where(t => t.Status == "Elérhető").ToList();
        trailerCombo.DisplayMemberPath = "BrandAndModel";

        var startDatePicker = new CalendarDatePicker { PlaceholderText = "Kezdő dátum", Height = 44, HorizontalAlignment = HorizontalAlignment.Stretch };
        var endDatePicker = new CalendarDatePicker { PlaceholderText = "Végdátum", Height = 44, HorizontalAlignment = HorizontalAlignment.Stretch };

        var summaryText = new TextBlock
        {
            Text = "Válassz utánfutót és dátumokat a számításhoz.",
            Margin = new Thickness(0, 16, 0, 0),
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };
      
        // Számolás és Validáció
        void UpdateSummary(object? sender, object? e)
        {
            dialog.IsPrimaryButtonEnabled = false;

            if (startDatePicker.Date.HasValue && endDatePicker.Date.HasValue && trailerCombo.SelectedItem is Trailer selectedTrailer)
            {
                var start = startDatePicker.Date!.Value.Date;
                var end = endDatePicker.Date.Value.Date;
                int days = (end - start).Days;

                bool isOverlap = bookedDates.Any(b => start <= b.End && end >= b.Start);

                if (days <= 0)
                {
                    summaryText.Text = "Hiba: A végdátumnak későbbinek kell lennie!";
                    summaryText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                }
                else if (isOverlap)
                {
                    summaryText.Text = "Hiba: Az időszak ütközik egy meglévő foglalással!";
                    summaryText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                }
                else
                {
                    decimal total = days * selectedTrailer.DailyRateFt;
                    summaryText.Text = $"Bérlés tartama: {days} nap\nFizetendő: {total:N0} Ft (+ {selectedTrailer.DepositFt:N0} Ft kaució)";
                    summaryText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);

                    // Engedélyezzük a mentést, ha minden ki van töltve
                    if (!string.IsNullOrWhiteSpace(nameBox.Text) && !string.IsNullOrWhiteSpace(phoneBox.Text))
                    {
                        dialog.IsPrimaryButtonEnabled = true;
                    }
                }
            }
        }

        startDatePicker.DateChanged += UpdateSummary;
        endDatePicker.DateChanged += UpdateSummary;
        nameBox.TextChanged += UpdateSummary;
        phoneBox.TextChanged += UpdateSummary;

        // Utánfutó váltás
        trailerCombo.SelectionChanged += async (s, e) =>
        {
            if (trailerCombo.SelectedItem is Trailer t)
            {
                var bookings = await _dbService.GetBookingsForTrailerAsync(t.Id);
                bookedDates.Clear();

                foreach (var booking in bookings)
                {
                    if (BookingTimeSlotMetadata.TryParseMultiDay(booking.TimeSlot, out var start, out var end))
                    {
                        bookedDates.Add((start.Date, end.Date));
                    }
                    else
                    {
                        bookedDates.Add((booking.BookingDate.Date, booking.BookingDate.Date));
                    }
                }

                UpdateSummary(null, null);
            }
        };

        // Űrlap összeállítása
        StackPanel CreateField(string label, Control input)
        {
            var sp = new StackPanel { Spacing = 4 };
            sp.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });
            sp.Children.Add(input);
            return sp;
        }

        var formPanel = new StackPanel
        {
            Spacing = 16,
            Width= 500,
            Margin = new Thickness(0, 0, 16, 0) 
        };

        formPanel.Children.Add(CreateField("Ügyfél neve", nameBox));
        formPanel.Children.Add(CreateField("Telefonszám", phoneBox));
        formPanel.Children.Add(CreateField("Email cím", emailBox));
        formPanel.Children.Add(CreateField("Utánfutó", trailerCombo));

        var dateGrid = new Grid { ColumnSpacing = 16 };
        dateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        dateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var startField = CreateField("Kezdő dátum", startDatePicker);
        var endField = CreateField("Végdátum", endDatePicker);

        Grid.SetColumn(startField, 0);
        Grid.SetColumn(endField, 1);

        dateGrid.Children.Add(startField);
        dateGrid.Children.Add(endField);

        formPanel.Children.Add(dateGrid);
        formPanel.Children.Add(summaryText);

        var scrollViewer = new ScrollViewer
        {
            Content = formPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Height = 450,
            MinWidth = 520
        };
        dialog.Content = scrollViewer;

        //Mentés
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var selTrailer = trailerCombo.SelectedItem as Trailer;
            var start = startDatePicker.Date!.Value.Date;
            var end = endDatePicker.Date!.Value.Date;
            int days = (end - start).Days;
            string timeSlot = days > 1 ? $"MULTI_DAY|{start:yyyy-MM-dd}|{end:yyyy-MM-dd}" : $"FULL_DAY|{start:yyyy-MM-dd}";

            var newBooking = new Booking
            {
                TrailerId = selTrailer!.Id,
                TrailerName = selTrailer.BrandAndModel,
                CustomerName = nameBox.Text,   
                CustomerPhone = phoneBox.Text, 
                Email = emailBox.Text,         
                BookingDate = start,
                TimeSlot = timeSlot,
                TotalPrice = days * selTrailer.DailyRateFt
            };

            try
            {
                if (_dbService != null)
                {
                    await _dbService.AddBookingAsync(newBooking);
                }

                await RefreshData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hiba a mentésnél: {ex.Message}");
            }
        }
    }
}
