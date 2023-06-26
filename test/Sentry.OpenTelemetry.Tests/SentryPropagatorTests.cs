using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Sentry.OpenTelemetry.Tests;

public class SentryPropagatorTests
{
    private ActivityContext _invalidContext = default;

    private static ActivityContext _validContext()
    {
        var sentryTraceHeader = new SentryTraceHeader(
            SentryId.Parse("5bd5f6d346b442dd9177dce9302fd737"),
            SpanId.Parse("b0d83d6cfec87606"),
            true
        );
        return new ActivityContext(
            sentryTraceHeader.TraceId.AsActivityTraceId(),
            sentryTraceHeader.SpanId.AsActivitySpanId(),
            sentryTraceHeader.IsSampled is true ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None,
            null,
            true
        );
    }

    private static IEnumerable<string> _getter(Dictionary<string, string> request, string key)
        => request.TryGetValue(key, out var value) ? new StringValues(value) : Enumerable.Empty<string>();

    [Fact]
    public void Inject_PropagationContext_To_Carrier()
    {
        // Arrange
        var activityContext = _validContext();
        var baggageIn = Baggage.Create(new Dictionary<string, string>());
        var contextIn = new PropagationContext(activityContext, baggageIn);
        var carrier = new Dictionary<string, string>();
        var sut = new SentryPropagator();

        // Act
        sut.Inject(contextIn, carrier, (c, k, v) => c[k] = v);

        // Assert
        carrier.Should().NotBeNull();
        using (new AssertionScope())
        {
            carrier.Should().ContainKey("sentry-trace");

            carrier["sentry-trace"].Should().Be("5bd5f6d346b442dd9177dce9302fd737-b0d83d6cfec87606-1");
        }
    }

    [Fact]
    public void Inject_PropagationContext_To_Baggage()
    {
        // Arrange
        var activityContext = _validContext();
        var baggageIn = Baggage.Create(new Dictionary<string, string>()
            {
                { "foo", "bar" }, // simulate some non-sentry baggage... this shouldn't be altered
            });
        var contextIn = new PropagationContext(activityContext, baggageIn);
        var carrier = new Dictionary<string, string>();
        var sut = new SentryPropagator();

        // Act
        sut.Inject(contextIn, carrier, (c, k, v) => c[k] = v);

        // Assert
        carrier.Should().NotBeNull();
        using (new AssertionScope())
        {
            carrier.Should().ContainKey("baggage");
            var baggageDictionary = (BaggageHeader.TryParse(carrier["baggage"])?.Members is {} members)
                ? new Dictionary<string, string>(members)
                : new Dictionary<string, string>();
            baggageDictionary.Should().Equal(new Dictionary<string, string>()
            {
                { "foo", "bar" },
            });
        }
    }

    [Fact]
    public void Inject_Invalid_Context_DoesNothing()
    {
        // Arrange
        var activityContext = _invalidContext;
        var baggageIn = new Baggage();
        var contextIn = new PropagationContext(activityContext, baggageIn);
        var carrier = new Dictionary<string, string>();
        var sut = new SentryPropagator();

        // Act
        sut.Inject(contextIn, carrier, (c, k, v) => c[k] = v);

        // Assert
        carrier.Should().NotBeNull();
        carrier.Should().BeEmpty();
    }

    [Fact]
    public void Extract_PropagationContext_From_Carrier()
    {
        // Arrange
        var carrier = new Dictionary<string, string>()
        {
            { "Accept", "*/*" },
            { "Connection", "keep-alive" },
            { "Host", "0.0.0.0" },
            { "User-Agent", "python-requests/2.31.0" },
            { "Accept-Encoding", "gzip, deflate" },
            {
                "baggage",
                "sentry-trace_id=5bd5f6d346b442dd9177dce9302fd737,sentry-environment=production,sentry-public_key=123,sentry-transaction=Pizza,sentry-sample_rate=1.0"
            },
            { "sentry-trace", "5bd5f6d346b442dd9177dce9302fd737-b0d83d6cfec87606-1" }
        };

        var sut = new SentryPropagator();
        var contextIn = new PropagationContext();

        // Act
        var outContext = sut.Extract(contextIn, carrier, _getter);

        // Assert
        outContext.Should().NotBeNull();
        outContext.ActivityContext.Should().NotBeNull();
        outContext.Baggage.Should().NotBeNull();
        using (new AssertionScope())
        {
            $"{outContext.ActivityContext.TraceId}".Should().Be("5bd5f6d346b442dd9177dce9302fd737");
            $"{outContext.ActivityContext.SpanId}".Should().Be("b0d83d6cfec87606");
            outContext.ActivityContext.TraceFlags.Should().Be(ActivityTraceFlags.Recorded);
            outContext.ActivityContext.TraceState.Should().BeNull();
            outContext.ActivityContext.IsRemote.Should().Be(true);

            outContext.Baggage.GetBaggage("sentry-trace_id").Should().Be("5bd5f6d346b442dd9177dce9302fd737");
            outContext.Baggage.GetBaggage("sentry-environment").Should().Be("production");
            outContext.Baggage.GetBaggage("sentry-public_key").Should().Be("123");
            outContext.Baggage.GetBaggage("sentry-transaction").Should().Be("Pizza");
            outContext.Baggage.GetBaggage("sentry-sample_rate").Should().Be("1.0");
        }
    }
}
