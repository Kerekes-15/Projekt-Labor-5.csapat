using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Extensions.DependencyInjection;
using VTrailer.Services;

namespace VTrailer.Presentation;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        this.InitializeComponent();
        NavView.SelectedItem = NavView.MenuItems[0];
        this.Loaded += HomePage_Loaded;
    }

    private void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        var user = DatabaseService.CurrentUser;

        if (user == null || (user.Role != "Adminisztrátor" && user.Role != "Alkalmazott"))
        {
            AllBookingsMenu.Visibility = Visibility.Collapsed;
        }
        else
        {
            AllBookingsMenu.Visibility = Visibility.Visible;
        }

        if (user == null || user.Role != "Adminisztrátor")
        {
            AuditLogMenu.Visibility = Visibility.Collapsed;
        }
        else
        {
            AuditLogMenu.Visibility = Visibility.Visible;
        }

        if (user == null || (user.Role != "Adminisztrátor"))
        {
            TrailerManagementMenu.Visibility = Visibility.Collapsed;
        }
        else
        {
            TrailerManagementMenu.Visibility = Visibility.Visible;
        }
    }

    private void ThemeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts && this.XamlRoot != null)
        {
            if (this.XamlRoot.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = ts.IsOn ? ElementTheme.Dark : ElementTheme.Light;
            }
        }
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var tag = args.InvokedItemContainer?.Tag?.ToString();

        switch (tag)
        {
            case "Home":
                ContentFrame.Content = null;
                WelcomeTextPanel.Visibility = Visibility.Visible;
                break;
            case "Trailer":
                WelcomeTextPanel.Visibility = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(TrailerPage));

                if (ContentFrame.Content is Page trailerPage)
                {
                    try
                    {
                        var host = (Application.Current as App)?.Host;
                        if (host != null)
                        {
                            using (var scope = host.Services.CreateScope())
                            {
                                trailerPage.DataContext = scope.ServiceProvider.GetService<TrailerViewModel>();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[HIBA] Nem sikerült betölteni a ViewModelt: {ex.Message}");
                    }
                }
                break;
            case "Booking":
                WelcomeTextPanel.Visibility = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(BookingPage));

                if (ContentFrame.Content is Page bookingPage)
                {
                    try
                    {
                        var host = (Application.Current as App)?.Host;
                        if (host != null)
                        {
                            using (var scope = host.Services.CreateScope())
                            {
                                bookingPage.DataContext = scope.ServiceProvider.GetService<BookingViewModel>();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[HIBA] Nem sikerült betölteni a foglalási ViewModelt: {ex.Message}");
                    }
                }
                break;
            case "MyBookingsPage":
                WelcomeTextPanel.Visibility = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(MyBookingsPage));
                break;
            case "AllBookingsPage":
                WelcomeTextPanel.Visibility = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(AllBookingsPage));
                break;
            case "TrailerManagementPage":
                WelcomeTextPanel.Visibility = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(TrailerManagementPage));
                break;
            case "AuditLog":
                WelcomeTextPanel.Visibility = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(AuditLog));
                break;
            case "Settings":
                WelcomeTextPanel.Visibility = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(ProfilePage));

                if (ContentFrame.Content is Page profilePage)
                {
                    try
                    {
                        var host = (Application.Current as App)?.Host;
                        if (host != null)
                        {
                            using (var scope = host.Services.CreateScope())
                            {
                                profilePage.DataContext = scope.ServiceProvider.GetService<ProfileViewModel>();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[HIBA] Nem sikerült betölteni a Profilt: {ex.Message}");
                    }
                }
                break;
            case "Logout":
                this.Frame?.Navigate(typeof(LoginPage));
                break;
        }
    }
}
