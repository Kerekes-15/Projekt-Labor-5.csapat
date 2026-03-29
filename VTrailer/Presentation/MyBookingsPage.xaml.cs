using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

        var currentUser = _dbService.CurrentUser;
        var username = currentUser?.Username ?? "tesztuser";

        try
        {
            var myBookings = await _dbService.GetMyBookingsAsync(username);

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
            EmptyStateText.Text = $"Hiba a foglalások betöltésekor: {ex.Message}";
            EmptyStateText.Visibility = Visibility.Visible;
            BookingsListView.Visibility = Visibility.Collapsed;
        }
    }
}
