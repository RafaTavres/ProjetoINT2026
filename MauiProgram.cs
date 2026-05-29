using Microsoft.Extensions.Logging;
using ProjetoINT2026.Services;

namespace ProjetoINT2026;

public static class MauiProgram
{
	public static IServiceProvider Services { get; private set; } = default!;

	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton(new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(8)
		});
		builder.Services.AddSingleton(new ChatApiOptions
		{
			// MVP: chamada direta ao Gemini pelo app.
			// Atenção: em app distribuido, essa chave pode ser extraida do APK.
			ApiKey = "",
			Model = "gemini-2.5-flash-lite"
		});
		builder.Services.AddSingleton<IHealthContentService, HealthContentService>();
		builder.Services.AddSingleton<IChatResponder, GeminiChatResponder>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		Services = app.Services;
		return app;
	}
}
