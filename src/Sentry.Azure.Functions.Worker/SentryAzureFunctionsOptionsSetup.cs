using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.Azure.Functions.Worker;

#if NET8_0_OR_GREATER
internal class SentryAzureFunctionsOptionsSetup : IConfigureOptions<SentryAzureFunctionsOptions>
{
    private readonly IConfiguration _config;

    public SentryAzureFunctionsOptionsSetup(IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
    }

    [RequiresDynamicCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(Object)")]
    [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(Object)")]
    public void Configure(SentryAzureFunctionsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var bindable = new BindableSentryAzureFunctionsOptions();
        _config.Bind(bindable);
        bindable.ApplyTo(options);

        // These can't be changed by the user
        options.TagFilters.Add("AzureFunctions_");
    }
}
#else
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
#endif
