using VTrailer.Models;
using VTrailer.Services;

namespace VTrailer.Presentation;

public partial class ProfileViewModel : ObservableObject
{
    private readonly INavigator _navigator;

    public User? CurrentUser { get; }

    public ProfileViewModel(INavigator navigator, DatabaseService databaseService)
    {
        _navigator = navigator;

        CurrentUser = databaseService.CurrentUser;
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await _navigator.NavigateBackAsync(this);
    }
}
