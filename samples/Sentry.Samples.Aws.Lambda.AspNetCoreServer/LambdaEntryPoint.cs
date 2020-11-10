using Microsoft.AspNetCore.Hosting;

// The entrypoint used when the function is deployed to AWS.
public class LambdaEntryPoint : Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        builder
            // Add Sentry (configuration was done via appsettings.json but could be done programatically):
            .UseSentry()
            .UseStartup<Startup>();
    }
}
