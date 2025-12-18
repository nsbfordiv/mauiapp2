using Microsoft.Identity.Client;
using MauiApp2.Services;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MauiApp2;

public partial class MainPage : ContentPage
{
    private readonly MsalClientService _msal;

    public MainPage(MsalClientService msal)
    {
        InitializeComponent();
        _msal = msal;
    }

    private static async Task<string?> GetOnPremSamAsync(string accessToken)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var json = await http.GetStringAsync(
            "https://graph.microsoft.com/v1.0/me?$select=onPremisesSamAccountName,userPrincipalName,displayName");

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("onPremisesSamAccountName", out var sam) &&
            sam.ValueKind == JsonValueKind.String)
        {
            return sam.GetString();
        }

        return null; // cloud-only users will typically land here
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var acct = (await _msal.PCA.GetAccountsAsync()).FirstOrDefault();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            SignInToolbarItem.Text = acct is null ? "Sign in" : acct.Username; // fallback
        });
    }

    private int _count = 0;

    private void OnCounterClicked(object sender, EventArgs e)
    {
        _count++;
        ((Button)sender).Text = $"Clicked {_count} times";
    }


    private async void OnSignInClicked(object sender, EventArgs e)
    {
        try
        {
            var scopes = new[] { "User.Read" };

            var result = await _msal.PCA
                .AcquireTokenInteractive(scopes)
#if ANDROID
                .WithParentActivityOrWindow(Platform.CurrentActivity)
#endif
                .ExecuteAsync();

            var sam = await GetOnPremSamAsync(result.AccessToken);
            var who = string.IsNullOrWhiteSpace(sam) ? result.Account.Username : sam;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                SignInToolbarItem.Text = who;
            });
        }
        catch (MsalException ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SignInToolbarItem.Text = "Sign in"; // or show error text if you want
            });
        }
    }
}
