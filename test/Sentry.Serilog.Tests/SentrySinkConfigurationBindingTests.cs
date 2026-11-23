using Microsoft.Extensions.Configuration;

namespace Sentry.Serilog.Tests;

public class SentrySinkConfigurationBindingTests
{
    private static IConfiguration SinkConfiguration(params (string Name, string Value)[] args)
    {
        var settings = new Dictionary<string, string>
        {
            ["Serilog:Using:0"] = "Sentry.Serilog",
            ["Serilog:WriteTo:0:Name"] = "Sentry"
        };

        foreach (var (name, value) in args)
        {
            settings[$"Serilog:WriteTo:0:Args:{name}"] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    private static Exception CreateLogger(IConfiguration configuration) => Record.Exception(() =>
        new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger().Dispose());

    [Fact]
    public void ReadFromConfiguration_WithDsn_Throws()
    {
        var exception = CreateLogger(SinkConfiguration(("dsn", ValidDsn)));

        var notSupported = Assert.IsType<NotSupportedException>(exception?.GetBaseException());
        Assert.Contains("SentrySdk.Init", notSupported.Message);
        Assert.Contains("UseSerilog()", notSupported.Message);
    }

    [Fact]
    public void ReadFromConfiguration_WithDsnAndSinkArguments_Throws()
    {
        var exception = CreateLogger(SinkConfiguration(("dsn", ValidDsn), ("minimumEventLevel", "Error")));

        Assert.IsType<NotSupportedException>(exception?.GetBaseException());
    }

    [Fact]
    public void ReadFromConfiguration_WithoutDsn_DoesNotThrow()
    {
        var exception = CreateLogger(SinkConfiguration(
            ("minimumEventLevel", "Error"),
            ("minimumBreadcrumbLevel", "Debug")));

        Assert.Null(exception);
    }

    [Fact]
    public void ReadFromConfiguration_WithoutArguments_DoesNotThrow()
    {
        var exception = CreateLogger(SinkConfiguration());

        Assert.Null(exception);
    }
}
