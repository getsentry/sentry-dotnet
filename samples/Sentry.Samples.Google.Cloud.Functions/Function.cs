using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

// NOTE: to run this sample you'll need to specify a Sentry DSN, either by configuring it in the
// appsettings.json file or by setting the SENTRY_DSN environment variable.
[assembly: FunctionsStartup(typeof(SentryStartup))]

public class Function : IHttpFunction
{
    private readonly ILogger<Function> _logger;
    public Function(ILogger<Function> logger) => _logger = logger;

    public Task HandleAsync(HttpContext context)
    {
        _logger.LogInformation("Useful info that is added to the breadcrumb list.");
        throw new Exception("Bad function");
    }
}
