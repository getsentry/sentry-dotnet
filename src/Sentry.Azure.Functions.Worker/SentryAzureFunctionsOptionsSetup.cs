using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.Azure.Functions.Worker;

internal class SentryAzureFunctionsOptionsSetup : ConfigureFromConfigurationOptions<SentryAzureFunctionsOptions>
{
    public SentryAzureFunctionsOptionsSetup(IConfiguration config) : base(config)
    {
    }

    public override void Configure(SentryAzureFunctionsOptions options)
    {
        // Mutable by user options

        base.Configure(options);

        // Immutable by user options

        options.TagFilters.Add("AzureFunctions_");
    }
}
