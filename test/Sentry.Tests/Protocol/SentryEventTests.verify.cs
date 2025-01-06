namespace Sentry.Tests.Protocol;

public partial class SentryEventTests
{
    [Fact]
    public async Task SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var ex = new Exception("exception message");
        var timestamp = DateTimeOffset.MaxValue;
        var id = Guid.Parse("4b780f4c-ec03-42a7-8ef8-a41c9d5621f8");
        var sut = new SentryEvent(ex, timestamp, id)
        {
            User = new SentryUser { Id = "user-id" },
            Request = new SentryRequest { Method = "POST" },
            Contexts = new SentryContexts
            {
                ["context_key"] = "context_value",
                [".NET Framework"] = new Dictionary<string, string>
                {
                    [".NET Framework"] = "\"v2.0.50727\", \"v3.0\", \"v3.5\"",
                    [".NET Framework Client"] = "\"v4.8\", \"v4.0.0.0\"",
                    [".NET Framework Full"] = "\"v4.8\""
                }
            },
            Sdk = new SdkVersion { Name = "SDK-test", Version = "1.1.1" },
            Environment = "environment",
            Level = SentryLevel.Fatal,
            Logger = "logger",
            Message = new SentryMessage
            {
                Message = "message",
                Formatted = "structured_message"
            },
            Modules = { { "module_key", "module_value" } },
            Release = "release",
            Distribution = "distribution",
            SentryExceptions = new[] { new SentryException { Value = "exception_value" } },
            SentryThreads = new[] { new SentryThread { Crashed = true } },
            ServerName = "server_name",
            TransactionName = "transaction",
            DebugImages = new List<DebugImage>
            {
                new()
                {
                    Type = "wasm",
                    DebugId = "900f7d1b868432939de4457478f34720"
                }
            },
        };

        sut.Sdk.AddPackage(new SentryPackage("name", "version"));
        sut.Sdk.AddIntegration("integration");
        sut.AddBreadcrumb(new Breadcrumb(timestamp, "crumb"));
        sut.AddBreadcrumb(new Breadcrumb(
            timestamp,
            "message",
            "type",
            new Dictionary<string, string> { { "data-key", "data-value" } },
            "category",
            BreadcrumbLevel.Warning));

        sut.SetExtra("extra_key", "extra_value");
        sut.Fingerprint = new[] { "fingerprint" };
        sut.SetTag("tag_key", "tag_value");

        var actualString = sut.ToJsonString(_testOutputLogger, indented: true);

        await VerifyJson(actualString);

        actualString.Should().Contain("""
              "debug_meta": {
                "images": [
                  {
                    "type": "wasm",
                    "debug_id": "900f7d1b868432939de4457478f34720"
                  }
                ]
              }
            """);

        var actual = Json.Parse(actualString, SentryEvent.FromJson);

        // Assert
        actual.Should().BeEquivalentTo(sut, o =>
        {
            // Exceptions are not deserialized
            o.Excluding(x => x.Exception);

            // Timestamps lose some precision when writing to JSON
            o.Using<DateTimeOffset>(ctx =>
                ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(1))
            ).WhenTypeIs<DateTimeOffset>();

            return o;
        });
    }
}
