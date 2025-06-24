// The entrypoint used when the function is deployed to AWS.
public class LambdaEntryPoint : Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        builder
#if !SENTRY_DSN_DEFINED_IN_ENV
            // A DSN is required. You can set it here in code, via the SENTRY_DSN environment variable or in your
            // appsettings.json file.
            // See https://docs.sentry.io/platforms/dotnet/guides/aspnetcore/#configure
            .UseSentry(SamplesShared.Dsn)
#else
            .UseSentry()
#endif
            // Add Sentry (configuration was done via appsettings.json but could be done programatically):
            .UseStartup<Startup>();
    }
}
