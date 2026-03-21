using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Uno.Extensions.Navigation;

namespace VTrailer.Presentation;

public class IntroViewModel : ObservableObject
{
    private readonly INavigator _navigator;

    public IntroViewModel(INavigator navigator)
    {
        _navigator = navigator;
        GoToLoginCommand = new AsyncRelayCommand(GoToLoginAsync);
    }

    public IAsyncRelayCommand GoToLoginCommand { get; }

    private async Task GoToLoginAsync()
    {
        await _navigator.NavigateViewModelAsync<LoginViewModel>(this);
    }
}
