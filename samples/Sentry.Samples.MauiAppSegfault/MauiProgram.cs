using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Sentry;

namespace MauiAppSegfault
{
    public static class MauiProgram
    {
        private const string Dsn = "https://60df419df8646e2fd04794313ec8018e@o447951.ingest.us.sentry.io/4507532630753280";

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSentry(options =>
                {
                    options.Dsn = Dsn;
                    options.Debug = true;
                });

            return builder.Build();
        }
    }
}
