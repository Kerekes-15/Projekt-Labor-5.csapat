using System.Windows.Input;

namespace VTrailer.Presentation;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authenticationService;
    private readonly INavigator _navigator;

    // Ezek maradtak le valószínűleg!
    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    private bool _hasError;
    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public LoginViewModel(IAuthenticationService authenticationService, INavigator navigator)
    {
        _authenticationService = authenticationService;
        _navigator = navigator;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        HasError = false; 

        var credentials = new Dictionary<string, string>
        {
           
            { "Username", Username ?? "" },
            { "Password", Password ?? "" }
        };

        
        var success = await _authenticationService.LoginAsync(credentials);

        if (success)
        {
            
            await _navigator.NavigateViewModelAsync<MainViewModel>(this, qualifier: Qualifiers.ClearBackStack);
        }
        else
        {
            
            HasError = true;
        }
    }
}
