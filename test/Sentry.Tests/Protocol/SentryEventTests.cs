namespace Sentry.Tests.Protocol;

public partial class SentryEventTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public SentryEventTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void Ctor_Platform_CSharp()
    {
        var evt = new SentryEvent();
        Assert.Equal(Constants.Platform, evt.Platform);
    }

    [Fact]
    public void Ctor_Timestamp_NonDefault()
    {
        var evt = new SentryEvent();
        Assert.NotEqual(default, evt.Timestamp);
    }

    [Fact]
    public void Ctor_EventId_NonDefault()
    {
        var evt = new SentryEvent();
        Assert.NotEqual(default, evt.EventId);
    }

    [Fact]
    public void Ctor_Exception_Stored()
    {
        var e = new Exception();
        var evt = new SentryEvent(e);
        Assert.Same(e, evt.Exception);
    }

    [Fact]
    public void SentryThreads_Getter_NotNull()
    {
        var evt = new SentryEvent();
        Assert.NotNull(evt.SentryThreads);
    }

    [Fact]
    public void SentryThreads_SetToNUll_Getter_NotNull()
    {
        var evt = new SentryEvent
        {
            SentryThreads = null
        };

        Assert.NotNull(evt.SentryThreads);
    }

    [Fact]
    public void SentryExceptions_Getter_NotNull()
    {
        var evt = new SentryEvent();
        Assert.NotNull(evt.SentryExceptions);
    }

    [Fact]
    public void SentryExceptions_SetToNUll_Getter_NotNull()
    {
        var evt = new SentryEvent
        {
            SentryExceptions = null
        };

        Assert.NotNull(evt.SentryExceptions);
    }

    [Fact]
    public void Modules_Getter_NotNull()
    {
        var evt = new SentryEvent();
        Assert.NotNull(evt.Modules);
    }

    [Fact]
    public void Redact_Redacts_Urls()
    {
        // Arrange
        var message = "message123 https://user@not.redacted";
        var logger = "logger123 https://user@not.redacted";
        var platform = "platform123 https://user@not.redacted";
        var serverName = "serverName123 https://user@not.redacted";
        var release = "release123 https://user@not.redacted";
        var distribution = "distribution123 https://user@not.redacted";
        var moduleValue = "module123 https://user@not.redacted";
        var transactionName = "transactionName123 https://user@sentry.io";
        var requestUrl = "https://user@not.redacted";
        var username = "username";
        var email = "bob@foo.com";
        var ipAddress = "127.0.0.1";
        var environment = "environment123 https://user@not.redacted";

        var breadcrumbMessage = "message https://user@sentry.io"; // should be redacted
        var breadcrumbDataValue = "data-value https://user@sentry.io"; // should be redacted
        var tagValue = "tag_value https://user@not.redacted";

        var timestamp = DateTimeOffset.MaxValue;

        var evt = new SentryEvent()
        {
            Message = message,
            Logger = logger,
            Platform = platform,
            ServerName = serverName,
            Release = release,
            Distribution = distribution,
            TransactionName = transactionName,
            Request = new SentryRequest
            {
                Method = "GET",
                Url = requestUrl
            },
            User = new SentryUser
            {
                Username = username,
                Email = email,
                IpAddress = ipAddress
            },
            Environment = environment,
        };
        evt.Modules.Add("module", moduleValue);
        evt.AddBreadcrumb(new Breadcrumb(timestamp, breadcrumbMessage));
        evt.AddBreadcrumb(new Breadcrumb(
            timestamp,
            "message",
            "type",
            new Dictionary<string, string> { { "data-key", breadcrumbDataValue } },
            "category",
            BreadcrumbLevel.Warning));
        evt.SetTag("tag_key", tagValue);

        // Act
        evt.Redact();

        // Assert
        using (new AssertionScope())
        {
            evt.Message.Message.Should().Be(message);
            evt.Logger.Should().Be(logger);
            evt.Platform.Should().Be(platform);
            evt.ServerName.Should().Be(serverName);
            evt.Release.Should().Be(release);
            evt.Distribution.Should().Be(distribution);
            evt.Modules["module"].Should().Be(moduleValue);
            evt.TransactionName.Should().Be(transactionName);
            // We don't redact the User or the Request since, if SendDefaultPii is false, we don't add these to the
            // transaction in the SDK anyway (by default they don't get sent... but the user can always override this
            // behavior if they need)
            evt.Request.Url.Should().Be(requestUrl);
            evt.User.Username.Should().Be(username);
            evt.User.Email.Should().Be(email);
            evt.User.IpAddress.Should().Be(ipAddress);
            evt.Environment.Should().Be(environment);
            var breadcrumbs = evt.Breadcrumbs.ToArray();
            breadcrumbs.Length.Should().Be(2);
            breadcrumbs[0].Message.Should().Be($"message https://{PiiExtensions.RedactedText}@sentry.io");
            breadcrumbs[1].Data?["data-key"].Should().Be($"data-value https://{PiiExtensions.RedactedText}@sentry.io");
            evt.Tags["tag_key"].Should().Be(tagValue);
        }
    }
}
