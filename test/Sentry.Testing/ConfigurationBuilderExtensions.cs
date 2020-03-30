using Microsoft.Extensions.Configuration;

namespace Sentry.Testing
{
    // As borrowed from: https://github.com/serilog/serilog-settings-configuration/blob/dev/test/Serilog.Settings.Configuration.Tests/Support/ConfigurationBuilderExtensions.cs
    // Serilog License: https://github.com/serilog/serilog-settings-configuration/blob/dev/LICENSE
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddJsonString(this IConfigurationBuilder builder, string json)
        {
            return builder.Add(new JsonStringConfigSource(json));
        }
    }
}
