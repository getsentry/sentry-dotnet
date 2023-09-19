using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.Azure.Functions.Worker;

internal class SentryAzure.FunctionsOptionsSetup : ConfigureFromConfigurationOptions<SentryAzure.FunctionsOptions>
{
    public SentryAzure.FunctionsOptionsSetup(IConfiguration config) : base(config)
    {
    }

    public override void Configure(SentryAzure.FunctionsOptions options)
    {
        // Mutable by user options

        base.Configure(options);

        // Immutable by user options

        options.TagFilters.Add("Azure.Functions_");
    }
}
