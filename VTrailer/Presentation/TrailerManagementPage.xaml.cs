using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using VTrailer.Models;
using VTrailer.Services;

namespace VTrailer.Presentation;

public sealed partial class TrailerManagementPage : Page
{
    private DatabaseService _dbService = new DatabaseService();
    public ObservableCollection<Trailer> Trailers { get; } = new ObservableCollection<Trailer>();

    public TrailerManagementPage()
    {
        this.InitializeComponent();
        TrailersListView.ItemsSource = Trailers;
        LoadDataAsync();
    }

    private async void LoadDataAsync()
    {
        var trailersList = await _dbService.GetTrailersAsync();
        TrailersListView.ItemsSource = null;

        Trailers.Clear();

        // ABC sorrendbe rendezés
        var sortedTrailers = trailersList.OrderBy(t => t.BrandAndModel).ToList();

        foreach (var trailer in sortedTrailers)
        {
            Trailers.Add(trailer);
        }

        TrailersListView.ItemsSource = Trailers;
    }

    private async void OnAddNewTrailerClick(object sender, RoutedEventArgs e)
    {
        await ShowTrailerDialogAsync(null); 
    }

    private async void OnEditTrailerClick(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is Trailer selectedTrailer)
        {
            await ShowTrailerDialogAsync(selectedTrailer); 
        }
    }

    private async Task ShowTrailerDialogAsync(Trailer? existingTrailer)
    {
        bool isEdit = existingTrailer != null;
        Windows.Storage.StorageFile? selectedFile = null;

        //UX segédfüggvények
        StackPanel CreateField(string label, Control input)
        {
            input.Height = 36;
            input.HorizontalAlignment = HorizontalAlignment.Stretch;
            var sp = new StackPanel { Spacing = 4 };
            sp.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });
            sp.Children.Add(input);
            return sp;
        }

        Grid CreateRow(UIElement left, UIElement right)
        {
            var grid = new Grid { ColumnSpacing = 16 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn((FrameworkElement)left, 0);
            Grid.SetColumn((FrameworkElement)right, 1);
            grid.Children.Add(left);
            grid.Children.Add(right);
            return grid;
        }

        //Beviteli mezők
        var brandBox = new TextBox { PlaceholderText = "pl. Eduard 3116", Text = existingTrailer?.BrandAndModel ?? "" };
        var plateBox = new TextBox { PlaceholderText = "pl. KAA-522", Text = existingTrailer?.LicensePlate ?? "" };
        var categoryBox = new TextBox { PlaceholderText = "pl. Nyitott", Text = existingTrailer?.Category ?? "" };
        var dailyRateBox = new TextBox { PlaceholderText = "pl. 8500", Text = existingTrailer?.DailyRateFt.ToString("0") ?? "" };
        var depositBox = new TextBox { PlaceholderText = "pl. 20000", Text = existingTrailer?.DepositFt.ToString("0") ?? "" };
        var payloadBox = new TextBox { PlaceholderText = "pl. 1500", Text = existingTrailer?.PayloadCapacityKg.ToString("0") ?? "" };
        var totalWeightBox = new TextBox { PlaceholderText = "pl. 1500", Text = existingTrailer?.TotalWeightKg.ToString("0") ?? "" };
        var lengthBox = new TextBox { PlaceholderText = "pl. 30", Text = existingTrailer?.InnerLengthCm.ToString("0") ?? "" };
        var widthBox = new TextBox { PlaceholderText = "pl. 140", Text = existingTrailer?.InnerWidthCm.ToString("0") ?? "" };
        var statusCombo = new ComboBox { ItemsSource = new[] { "Elérhető", "Szervizben" }, SelectedItem = existingTrailer?.Status ?? "Elérhető" };

        //Előnézet
        string currentImagePath = existingTrailer?.ImageUrl ?? "";
        var imagePreview = new Image
        {
            Source = !string.IsNullOrEmpty(currentImagePath) ? new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(currentImagePath)) : null,
            Height = 200,
            Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
        };
        var imageBorder = new Border { CornerRadius = new CornerRadius(8), Child = imagePreview, Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SurfaceVariantBrush"] };

        var browseButton = new Button
        {
            Content = "Kép kiválasztása...",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 40,
            Style = (Microsoft.UI.Xaml.Style)Application.Current.Resources["AccentButtonStyle"] // Kék/Kiemelt gomb
        };

        browseButton.Click += async (s, e) =>
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            var app = (App)Application.Current;
            if (app.MainWindow != null) WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(app.MainWindow));
            picker.FileTypeFilter.Add(".jpg"); picker.FileTypeFilter.Add(".png");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                selectedFile = file;
                using var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                await bitmap.SetSourceAsync(stream);
                imagePreview.Source = bitmap;
            }
        };

        //Form összeállítása
        var formPanel = new StackPanel { Spacing = 12, Width = 450 }; // Kényelmes, egysoros szélesség
        formPanel.Children.Add(CreateField("Márka és Típus *", brandBox));
        formPanel.Children.Add(CreateRow(CreateField("Rendszám", plateBox), CreateField("Kategória", categoryBox)));
        formPanel.Children.Add(CreateField("Státusz", statusCombo));
        formPanel.Children.Add(CreateRow(CreateField("Napi díj (Ft) *", dailyRateBox), CreateField("Kaució (Ft)", depositBox)));
        formPanel.Children.Add(CreateRow(CreateField("Terhelhetőség (kg)", payloadBox), CreateField("Össztömeg (kg)", totalWeightBox)));
        formPanel.Children.Add(CreateRow(CreateField("Belső hossz (cm)", lengthBox), CreateField("Belső szélesség (cm)", widthBox)));

        // Képes szekció a legvégén
        var imageSection = new StackPanel { Spacing = 8, Margin = new Thickness(0, 16, 0, 0) };
        imageSection.Children.Add(new TextBlock { Text = "Utánfutó fotója", FontSize = 12, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });
        imageSection.Children.Add(imageBorder);
        imageSection.Children.Add(browseButton);
        formPanel.Children.Add(imageSection);

        //Scroll
        var scrollViewer = new ScrollViewer
        {
            Content = formPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 650, 
            Padding = new Thickness(0, 0, 16, 16)
        };

        var dialog = new ContentDialog
        {
            Title = isEdit ? "Utánfutó szerkesztése" : "Új utánfutó hozzáadása",
            Content = scrollViewer,
            PrimaryButtonText = "Mentés",
            CloseButtonText = "Mégse",
            XamlRoot = this.XamlRoot
        };

        //Mentés
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            try
            {
                var trailer = isEdit ? existingTrailer! : new Trailer();

                //Adatok betöltése
                trailer.BrandAndModel = brandBox.Text;
                trailer.LicensePlate = plateBox.Text;
                trailer.Category = categoryBox.Text;
                trailer.Status = statusCombo.SelectedItem?.ToString() ?? "Elérhető";

                decimal.TryParse(dailyRateBox.Text, out decimal rate); trailer.DailyRateFt = rate;
                decimal.TryParse(depositBox.Text, out decimal dep); trailer.DepositFt = dep;
                int.TryParse(payloadBox.Text, out int pl); trailer.PayloadCapacityKg = pl;
                int.TryParse(totalWeightBox.Text, out int tw); trailer.TotalWeightKg = tw;
                double.TryParse(lengthBox.Text, out double l); trailer.InnerLengthCm = l;
                double.TryParse(widthBox.Text, out double w); trailer.InnerWidthCm = w;

                //Kép feltöltése
                if (selectedFile != null)
                {
                    string publicUrl = await _dbService.UploadTrailerImageAsync(selectedFile);
                    if (!string.IsNullOrWhiteSpace(publicUrl))
                    {
                        trailer.ImageUrl = publicUrl; 
                    }
                }
                //Adatbázisnak küldés
                if (isEdit) await _dbService.UpdateTrailerAsync(trailer);
                else await _dbService.AddTrailerAsync(trailer);

                //Lista frissítés
                LoadDataAsync();
            }
            catch (Exception ex)
            {
                //Védőháló: Ha bármi összeomlik, ezt az ablakot fogod látni!
                var errorDialog = new ContentDialog
                {
                    Title = "Hiba történt a mentés során!",
                    Content = $"Részletek:\n{ex.Message}",
                    CloseButtonText = "Rendben",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
    }
    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var trailer = button?.Tag as Trailer;

        if (trailer != null)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Utánfutó törlése",
                Content = $"Biztosan törölni szeretnéd a következőt: {trailer.BrandAndModel}?",
                PrimaryButtonText = "Igen, törlés",
                CloseButtonText = "Mégse",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await _dbService.DeleteTrailerAsync(trailer.Id); 
                LoadDataAsync(); 
            }
        }
    }
}
