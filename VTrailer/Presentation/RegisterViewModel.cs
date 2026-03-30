using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using VTrailer.Services;

namespace VTrailer.Presentation;

public partial class RegisterViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly INavigator _navigator;

    // --- BOMBABIZTOS VÁLTOZÓK ---
    private string _fullName = "";
    public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }

    private string _email = "";
    public string Email { get => _email; set => SetProperty(ref _email, value); }

    private string _phoneNumber = "";
    public string PhoneNumber { get => _phoneNumber; set => SetProperty(ref _phoneNumber, value); }

    private string _password = "";
    public string Password { get => _password; set => SetProperty(ref _password, value); }

    private string _confirmPassword = "";
    public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }

    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    private bool _hasError;
    public bool HasError { get => _hasError; set => SetProperty(ref _hasError, value); }

    public RegisterViewModel(DatabaseService databaseService, INavigator navigator)
    {
        _databaseService = databaseService;
        _navigator = navigator;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {

        // 1. Kiszedünk minden szóközt és kötőjelet, amit a user véletlenül beírt
        string tisztaSzam = PhoneNumber.Replace(" ", "").Replace("-", "").Replace("/", "");

        // 2. Ha "06"-tal kezdte (pl. 06301234567), átírjuk nemzetközi "+36"-ra
        if (tisztaSzam.StartsWith("06"))
        {
            tisztaSzam = "+36" + tisztaSzam.Substring(2);
        }

        // 3. Utána a modellt már ezzel a tisztaSzam-mal mentjük el a Supabase-be!
        HasError = false;
        ErrorMessage = "";

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(FullName))
        {
            ErrorMessage = "A Név, az E-mail és a Jelszó megadása kötelező!";
            HasError = true; return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "A két jelszó nem egyezik!";
            HasError = true; return;
        }

        // Hívjuk az ÚJ 4 paraméteres szervizt
        var success = await _databaseService.RegisterUserAsync(Email, Password, FullName, PhoneNumber);

        if (success)
        {
            await _navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
        }
        else
        {
            ErrorMessage = "Hiba a regisztráció során! (Talán már létezik ez az e-mail?)";
            HasError = true;
        }
    }

    [RelayCommand]
    private async Task GoToLoginAsync()
    {
        // Ez a parancs visz vissza a Login oldalra (ezt kötjük a Vissza gombra is)
        await _navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
    }
}
