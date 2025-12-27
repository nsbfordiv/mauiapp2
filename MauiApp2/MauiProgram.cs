using Microsoft.Extensions.Logging;
using MauiApp2.Services;


namespace MauiApp2;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine("UnhandledException: " + e.ExceptionObject);
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine("UnobservedTaskException: " + e.Exception);
            e.SetObserved();
        };

        builder
            .UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

	builder.Services.AddSingleton<MsalClientService>();
	builder.Services.AddSingleton<MainPage>();
	builder.Services.AddSingleton<AppShell>();
    builder.Services.AddTransient<MainPage>();



        return builder.Build();
	}
}
