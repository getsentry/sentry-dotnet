using System;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using Sentry;

namespace Sentry.Samples.Maui
{
    public class Startup : IStartup
    {
        public void Configure(IAppHostBuilder appBuilder)
        {
            appBuilder
                .UseMauiApp<App>()
                .UseSentry(o =>
                {
                    o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
                    o.Debug = true;
                    o.CacheDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });
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
