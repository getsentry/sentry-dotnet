using System;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Compatibility;
using Sentry;

namespace Sentry.Samples.Maui
{
	public class Startup : IStartup
	{
		public void Configure(IAppHostBuilder appBuilder)
		{
			appBuilder
				.UseFormsCompatibility()
                .UseSentry(o =>
                {
                    o.Dsn = "https://80aed643f81249d4bed3e30687b310ab@o447951.ingest.sentry.io/5428537";
                })
				.UseMauiApp<App>();
		}
	}
}

internal static class SentryAppHostBuilderExtensions
{
    public static IAppHostBuilder UseSentry(this IAppHostBuilder builder, Action<SentryOptions> configure)
    {
        // TODO: builder.Properties
        SentrySdk.Init(o =>
        {
            // o.CacheDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            configure(o);
        });
        return builder;
    }
}
