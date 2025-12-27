using MauiApp2.Services;

namespace MauiApp2;

public partial class AppShell : Shell
{
    private readonly ShellViewModel _vm;

    public AppShell(MsalClientService msal)
    {
        InitializeComponent();
        _vm = new ShellViewModel(msal);
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.RefreshSignedInUserAsync();
    }
}
