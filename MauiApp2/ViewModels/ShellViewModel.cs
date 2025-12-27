using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiApp2.Services;

namespace MauiApp2;

public class ShellViewModel : INotifyPropertyChanged
{
    private readonly MsalClientService _msal;

    private string _signedInUser = "";
    public string SignedInUser
    {
        get => _signedInUser;
        private set
        {
            if (_signedInUser == value) return;
            _signedInUser = value;
            OnPropertyChanged();
        }
    }

    public ICommand AccountTapCommand { get; }

    public ShellViewModel(MsalClientService msal)
    {
        _msal = msal;

        AccountTapCommand = new Command(async () => await OnAccountTapAsync());
    }

    public async Task RefreshSignedInUserAsync()
    {
        var name = await _msal.GetUsernameForUiAsync();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            SignedInUser = string.IsNullOrWhiteSpace(name) ? "Sign in" : name;
        });
    }

    private async Task OnAccountTapAsync()
    {
        if (SignedInUser == "Sign in")
        {
            try
            {
                await _msal.SignInInteractiveAsync();
            }
            catch
            {
                // user cancelled or sign-in failed
            }

            await RefreshSignedInUserAsync();
            return;
        }

        // Signed in -> sign out
        await _msal.SignOutAsync();
        await RefreshSignedInUserAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}
