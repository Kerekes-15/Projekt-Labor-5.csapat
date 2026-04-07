namespace VTrailer.Presentation;

public sealed partial class LoginPage : Page
{
    private bool _isPasswordVisible;

    public LoginPage()
    {
        this.InitializeComponent();
    }

    private void TogglePasswordVisibilityButton_Click(object sender, RoutedEventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        LoginPasswordBox.PasswordRevealMode = _isPasswordVisible
            ? Microsoft.UI.Xaml.Controls.PasswordRevealMode.Visible
            : Microsoft.UI.Xaml.Controls.PasswordRevealMode.Hidden;
        TogglePasswordVisibilityButton.Content = _isPasswordVisible ? "Elrejt" : "Mutat";
    }
}
