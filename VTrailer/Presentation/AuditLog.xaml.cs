using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VTrailer; 
using VTrailer.Services;

namespace VTrailer.Presentation;


public sealed partial class AuditLog : Page
{
    private DatabaseService _dbService = new DatabaseService();

    public AuditLog() 
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var user = DatabaseService.CurrentUser;

        
        if (user == null || user.Role != "Adminisztrátor")
        {
            Frame.Navigate(typeof(HomePage));
            return;
        }

        var logs = await _dbService.GetAuditLogsAsync();
        LogListView.ItemsSource = logs;
    }
}
