namespace Sentry.Tests.Internals;

[UsesVerify]
public class EventScrubberTests
{
    [Fact]
    public Task ScrubEvent_ScrubsRequest()
    {
        // Arrange
        const string data = """
                            {
                                "foo": "bar"
                            }
                            """;
        var ev = new SentryEvent
        {
            Request = new SentryRequest()
            {
                Url = "http://absolute.uri/foo",
                Method = "POST",
                ApiTarget = "apiType",
                Data = data,
                QueryString = "hello=world",
                Cookies = "foo=bar; secret=squirrel",
                Headers = {
                    {"Content-Type", "text/html"},
                    {"apikey", "myapikey"}
                },
                Env = {
                    {"REMOTE_ADDR", "192.168.0.1"}
                }
            }
        };

        // Act
        var scrubber = new EventScrubber();
        scrubber.ScrubEvent(ev);

        // Assert
        return Verify(ev.Request);
    }

    [Fact]
    public void ScrubEvent_ScrubsExtra()
    {
        // Arrange
        var ev = new SentryEvent();
        ev.SetExtras(new Dictionary<string, object>(){
            {"foo", "bar"},
            {"apikey", "myapikey"},
            {"session", new object()}
        });

        // Act
        var scrubber = new EventScrubber();
        scrubber.ScrubEvent(ev);

        // Assert
        ev.Extra["foo"].Should().Be("bar");
        ev.Extra["apikey"].Should().Be(EventScrubber.ScrubbedText);
        ev.Extra["session"].Should().Be(EventScrubber.ScrubbedText);
    }

    [Fact]
    public void ScrubEvent_ScrubsUser()
    {
        // Arrange
        var ev = new SentryEvent
        {
            User =
            {
                Other = new Dictionary<string, string>(){
                    {"foo", "bar"},
                    {"apikey", "myapikey"}
                }
            }
        };

        // Act
        var scrubber = new EventScrubber();
        scrubber.ScrubEvent(ev);

        // Assert
        ev.User.Other["foo"].Should().Be("bar");
        ev.User.Other["apikey"].Should().Be(EventScrubber.ScrubbedText);
    }

    [Fact]
    public void ScrubEvent_ScrubsBreadcrumbs()
    {
        // Arrange
        var ev = new SentryEvent();
        ev.AddBreadcrumb(new Breadcrumb("message", "type", new Dictionary<string, string>
        {
            {"foo", "bar"},
            {"apikey", "myapikey"}
        }));

        // Act
        var scrubber = new EventScrubber();
        scrubber.ScrubEvent(ev);

        // Assert
        var breadcrumb = ev.Breadcrumbs.FirstOrDefault();
        breadcrumb.Should().NotBeNull();
        using (new AssertionScope())
        {
            breadcrumb!.Message.Should().Be("message");
            breadcrumb.Type.Should().Be("type");
            breadcrumb.Data!["foo"].Should().Be("bar");
            breadcrumb.Data!["apikey"].Should().Be(EventScrubber.ScrubbedText);
        }
    }

    [Fact]
    public void ScrubEvent_ScrubsStackFrames()
    {
        // Arrange
        var ev = new SentryEvent
        {
            SentryExceptionValues = new SentryValues<SentryException>([
                new SentryException
                {
                    Stacktrace = new SentryStackTrace
                    {
                        Frames = [new SentryStackFrame()]
                    }
                }
            ])
        };
        ev.SentryExceptions!.First().Stacktrace!.Frames[0].Vars["foo"] = "bar";
        ev.SentryExceptions!.First().Stacktrace!.Frames[0].Vars["password"] = "42";

        // Act
        var scrubber = new EventScrubber();
        scrubber.ScrubEvent(ev);

        // Assert
        var exception = ev.SentryExceptions!.FirstOrDefault();
        var frame = exception!.Stacktrace!.Frames.FirstOrDefault();
        frame!.InternalVars!["foo"].Should().Be("bar");
        frame!.InternalVars!["password"].Should().Be(EventScrubber.ScrubbedText);
    }
}
