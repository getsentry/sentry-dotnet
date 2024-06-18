using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry.Benchmarks;

[SimpleJob]
public class EventScrubberBenchmarks
{
    private SentryEvent _ev;
    private EventScrubber _scrubber;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _scrubber = new EventScrubber(); // executed once per each N value
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _ev = new SentryEvent
        {
            Request = new SentryRequest()
            {
                Url = "http://absolute.uri/foo",
                Method = "POST",
                ApiTarget = "apiType",
                QueryString = "hello=world",
                Cookies = "foo=bar; secret=squirrel",
                Headers = {
                    {"Content-Type", "text/html"},
                    {"apikey", "myapikey"}
                },
                Env = {
                    {"REMOTE_ADDR", "192.168.0.1"}
                }
            },
            SentryExceptionValues = new SentryValues<SentryException>([
                new SentryException
                {
                    Stacktrace = new SentryStackTrace
                    {
                        Frames = [new SentryStackFrame()]
                    }
                }
            ]),
            User =
            {
                Other = new Dictionary<string, string>(){
                    {"foo", "bar"},
                    {"apikey", "myapikey"}
                }
            }
        };
        _ev.SentryExceptions!.First().Stacktrace!.Frames[0].Vars["foo"] = "bar";
        _ev.SentryExceptions!.First().Stacktrace!.Frames[0].Vars["password"] = "42";
        for (var i = 0; i < 10; i++)
        {
            _ev.AddBreadcrumb(new Breadcrumb("message", "type", new Dictionary<string, string>
            {
                {"foo", "bar"},
                {"apikey", "myapikey"}
            }));
        }
        _ev.SetExtras(new Dictionary<string, object>(){
            {"foo", "bar"},
            {"apikey", "myapikey"},
            {"session", new object()}
        });
    }

    [Benchmark]
    public void ScrubEvent_DefaultDenyList()
    {
        _scrubber.ScrubEvent(_ev);
    }
}
