using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using VTrailer.Models;
using VTrailer.Services;

namespace VTrailer.Presentation;

public partial class ProfileViewModel : ObservableObject
{
    private readonly INavigator _navigator;
    private readonly DatabaseService _databaseService;

    private User _dbUserRecord = new();

    private string _fullName = "";
    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
    }

    private string _email = "";
    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    private string _phoneNumber = "";
    public string PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    private string _role = "";
    public string Role
    {
        get => _role;
        set => SetProperty(ref _role, value);
    }

    private string _newPassword = "";
    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    private string _confirmNewPassword = "";
    public string ConfirmNewPassword
    {
        get => _confirmNewPassword;
        set => SetProperty(ref _confirmNewPassword, value);
    }

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    private string _passwordStatusMessage = "";
    public string PasswordStatusMessage
    {
        get => _passwordStatusMessage;
        set => SetProperty(ref _passwordStatusMessage, value);
    }

    public ProfileViewModel(INavigator navigator, DatabaseService databaseService)
    {
        _navigator = navigator;
        _databaseService = databaseService;

        _ = LoadProfileDataAsync();
    }

    private async Task LoadProfileDataAsync()
    {
        string currentUserEmail = DatabaseService.CurrentUser?.Email ?? "";
        if (string.IsNullOrEmpty(currentUserEmail)) return;

        var fetchedUser = await _databaseService.GetUserProfileAsync(currentUserEmail);

        if (fetchedUser != null)
        {
            _dbUserRecord = fetchedUser;
            FullName = _dbUserRecord.FullName ?? "";
            Email = _dbUserRecord.Email ?? currentUserEmail;
            PhoneNumber = FormatPhoneNumber(_dbUserRecord.PhoneNumber ?? "");
            Role = _dbUserRecord.Role ?? "User";
        }
        else
        {
            _dbUserRecord.Email = currentUserEmail;
            Email = currentUserEmail;
            Role = "User";
        }
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        StatusMessage = "Mentés folyamatban...";
        PhoneNumber = FormatPhoneNumber(PhoneNumber);
        _dbUserRecord.FullName = FullName;
        _dbUserRecord.Email = Email;
        _dbUserRecord.PhoneNumber = PhoneNumber;
        _dbUserRecord.Role = Role;

        bool success = await _databaseService.SaveUserProfileAsync(_dbUserRecord);
        StatusMessage = success ? "Sikeresen mentve! ✅" : "Hiba történt a mentés során! ❌";
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        // 1. Ellenőrzések
        if (string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmNewPassword))
        {
            PasswordStatusMessage = "Kérlek, töltsd ki mindkét jelszó mezőt!";
            return;
        }

        if (NewPassword != ConfirmNewPassword)
        {
            PasswordStatusMessage = "A két jelszó nem egyezik!";
            return;
        }

        if (NewPassword.Length < 6)
        {
            PasswordStatusMessage = "A jelszónak legalább 6 karakternek kell lennie!";
            return;
        }

        // 2. Mentés a felhőbe
        PasswordStatusMessage = "Jelszó frissítése folyamatban...";

        // Figyelem: Itt a saját db service-ed nevét használd! (pl. _dbService vagy _databaseService)
        bool success = await _databaseService.ChangePasswordAsync(NewPassword);

        // 3. Visszajelzés
        if (success)
        {
            PasswordStatusMessage = "Jelszó sikeresen frissítve!";
            NewPassword = ""; // Kiürítjük a mezőket a siker után
            ConfirmNewPassword = "";
        }
        else
        {
            PasswordStatusMessage = "Hiba történt a jelszó frissítésekor.";
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await _navigator.NavigateBackAsync(this);
    }

    private string FormatPhoneNumber(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "";

        string clean = phone.Replace(" ", "").Replace("-", "").Replace("/", "");

        if (clean.StartsWith("06")) clean = "+36" + clean.Substring(2);
        else if (clean.StartsWith("36")) clean = "+" + clean;

        if (clean.Length == 12 && clean.StartsWith("+36"))
        {
            return $"{clean.Substring(0, 3)} {clean.Substring(3, 2)} {clean.Substring(5, 3)} {clean.Substring(8, 4)}";
        }

        return clean; 
    }
}
