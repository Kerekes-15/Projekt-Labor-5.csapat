using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Uno.Extensions.Navigation;
using System.Threading.Tasks;

namespace VTrailer.Presentation;

public class IntroViewModel : ObservableObject
{
    private readonly INavigator _navigator;

    public IntroViewModel(INavigator navigator)
    {
        _navigator = navigator;
        GoToLoginCommand = new AsyncRelayCommand(GoToLoginAsync);
    }

    public ICommand GoToLoginCommand { get; }

    private async Task GoToLoginAsync()
    {
        await _navigator.NavigateViewModelAsync<LoginViewModel>(this);
    }
}
