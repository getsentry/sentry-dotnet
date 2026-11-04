using Microsoft.Extensions.Configuration;

namespace Sentry.Extensions.Logging.Tests;

// Binding from this assembly uses the reflection binder, which is what .NET Framework gets: the configuration
// binding source generator only runs for net8.0 and later.
public class BindableDataCollectionReflectionTests
{
    [Fact]
    public void Bind_DataCollection_WithReflectionBinder_Applies()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["DataCollection:Cookies:Mode"] = "Off",
                ["DataCollection:UrlQueryParams:Terms:0"] = "session_id",
                ["DataCollection:HttpBodies:0"] = "IncomingRequest",
                ["DataCollection:HttpBodies:1"] = "OutgoingResponse",
            })
            .Build();
        var bindable = new BindableSentryLoggingOptions();
        var options = new SentryLoggingOptions();

        config.Bind(bindable);
        bindable.ApplyTo(options);

        using (new AssertionScope())
        {
            options.DataCollection!.Cookies!.Mode.Should().Be(KeyValueCollectionMode.Off);
            options.DataCollection.UrlQueryParams!.Terms.Should().Equal("session_id");
            options.DataCollection.HttpBodies.Should().Be(
                HttpBodyTypes.IncomingRequest | HttpBodyTypes.OutgoingResponse);
        }
    }
}
