using Microsoft.Identity.Client;

namespace MauiApp2.Services;

public class MsalClientService
{
    public IPublicClientApplication PCA { get; }

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
}