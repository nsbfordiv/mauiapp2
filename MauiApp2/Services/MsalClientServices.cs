using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;

namespace MauiApp2.Services;

public class MsalClientService
{
    public IPublicClientApplication PCA { get; }

    private static readonly string[] Scopes = new[] { "User.Read" };

    public MsalClientService()
    {
        const string clientId = "99cbc75f-9f93-4780-bdb5-19cdf8f81560";
        const string tenantId = "303ad296-601c-4851-8aa3-53c909c823a0";

#if ANDROID
        var redirectUri = "msauth://com.cannonwendt.com.mauiapp2/4IorUsBmXGtTquTeQUbQJjTPYRI%3D";
#elif IOS
        var redirectUri = "msauth.com.cannon-wendt.com.mauiapp2://auth";
#else
        var redirectUri = $"msal{clientId}://auth";
#endif

        var pcaBuilder = PublicClientApplicationBuilder
            .Create(clientId)
            .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
            .WithRedirectUri(redirectUri);

#if ANDROID
        pcaBuilder = pcaBuilder.WithBroker(true);
#endif

        PCA = pcaBuilder.Build();
    }

    /// <summary>
    /// Returns username suitable for UI: prefer onPremisesSamAccountName, else local-part of UPN.
    /// Works on cold start when the user is still signed in (account cached).
    /// </summary>
    public async Task<string?> GetUsernameForUiAsync()
    {
        var account = (await PCA.GetAccountsAsync().ConfigureAwait(false)).FirstOrDefault();
        if (account is null)
            return null;

        // Fallback if we can't get a token silently
        var upn = account.Username ?? string.Empty;

        try
        {
            var result = await PCA
                .AcquireTokenSilent(Scopes, account)
                .ExecuteAsync()
                .ConfigureAwait(false);

            upn = result.Account?.Username ?? upn;

            // Optional best-case: hybrid SAM from Graph
            var sam = await GetOnPremSamAsync(result.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(sam))
                return sam;
        }
        catch (MsalUiRequiredException)
        {
            // Token requires interaction; still show friendly name from cached UPN.
        }

        return LocalPart(upn);
    }

    private static string LocalPart(string upn)
    {
        if (string.IsNullOrWhiteSpace(upn)) return string.Empty;
        var at = upn.IndexOf('@');
        return at > 0 ? upn[..at] : upn;
    }

    private static async Task<string?> GetOnPremSamAsync(string accessToken)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var json = await http.GetStringAsync(
            "https://graph.microsoft.com/v1.0/me?$select=onPremisesSamAccountName");

        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("onPremisesSamAccountName", out var sam) &&
            sam.ValueKind == JsonValueKind.String)
        {
            return sam.GetString();
        }

        return null;
    }
    public async Task<AuthenticationResult> SignInInteractiveAsync(bool forcePrompt = false)
    {
        var builder = PCA.AcquireTokenInteractive(Scopes);

#if ANDROID
        builder = builder.WithParentActivityOrWindow(Platform.CurrentActivity);
#endif

        if (forcePrompt)
        {
            // Better for testing than ForceLogin; it makes the user pick an account
            builder = builder.WithPrompt(Prompt.SelectAccount);
            // If you truly want a password prompt every time, use:
            // builder = builder.WithPrompt(Prompt.ForceLogin);
        }

        return await builder.ExecuteAsync().ConfigureAwait(false);
    }


    public async Task SignOutAsync()
    {
        var accounts = await PCA.GetAccountsAsync().ConfigureAwait(false);

        foreach (var acct in accounts)
        {
            await PCA.RemoveAsync(acct).ConfigureAwait(false);
        }
    }


    public async Task<bool> IsSignedInAsync()
    {
        var acct = (await PCA.GetAccountsAsync().ConfigureAwait(false)).FirstOrDefault();
        return acct is not null;
    }
}
