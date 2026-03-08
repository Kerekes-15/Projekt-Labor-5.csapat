using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Uno.Extensions.Hosting;
using VTrailer.Models;
using VTrailer.Services;

namespace VTrailer.Presentation;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigator _navigator;
    private readonly IAuthenticationService _authenticationService;
    private readonly DatabaseService _databaseService;
    private readonly IThemeService _themeService;

    public ObservableCollection<Trailer> Trailers { get; } = new ObservableCollection<Trailer>();

    private bool _isDarkMode;
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (SetProperty(ref _isDarkMode, value))
            {
                _themeService.SetThemeAsync(value ? AppTheme.Dark : AppTheme.Light);
            }
        }
    }

    public MainViewModel(
        INavigator navigator,
        IAuthenticationService authenticationService,
        DatabaseService databaseService,
        IThemeService themeService)
    {
        _navigator = navigator;
        _authenticationService = authenticationService;
        _databaseService = databaseService;
        _themeService = themeService;

        _isDarkMode = _themeService.IsDark;

        LoadDataAsync();
    }

    private async void LoadDataAsync()
    {
        var data = await _databaseService.GetTrailersAsync();
        Trailers.Clear();
        foreach (var item in data)
        {
            Trailers.Add(item);
        }
    }

    // --- NAVIGÁCIÓS PARANCSOK ---

    [RelayCommand]
    private async Task GoToProfileAsync()
    {
        await _navigator.NavigateViewModelAsync<ProfileViewModel>(this);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _authenticationService.LogoutAsync();
        await _navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
    }
}
