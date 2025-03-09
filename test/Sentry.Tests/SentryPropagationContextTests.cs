namespace Sentry.Tests;

public class SentryPropagationContextTests
{
    [Fact]
    public void CopyConstructor_CreatesCopy()
    {
        var original = new SentryPropagationContext();
        original.GetOrCreateDynamicSamplingContext(new SentryOptions { Dsn = ValidDsn });

        var copy = new SentryPropagationContext(original);

        Assert.Equal(original.TraceId, copy.TraceId);
        Assert.Equal(original.SpanId, copy.SpanId);
        Assert.Equal(original._dynamicSamplingContext, copy._dynamicSamplingContext);
    }

    [Fact]
    public void GetOrCreateDynamicSamplingContext_DynamicSamplingContextIsNull_CreatesDynamicSamplingContext()
    {
        var options = new SentryOptions { Dsn = ValidDsn };
        var propagationContext = new SentryPropagationContext();

        Assert.Null(propagationContext._dynamicSamplingContext); // Sanity check
        _ = propagationContext.GetOrCreateDynamicSamplingContext(options);

        Assert.NotNull(propagationContext._dynamicSamplingContext);
    }

    [Fact]
    public void GetOrCreateDynamicSamplingContext_DynamicSamplingContextIsNotNull_ReturnsSameDynamicSamplingContext()
    {
        var options = new SentryOptions { Dsn = ValidDsn };
        var propagationContext = new SentryPropagationContext();
        var firstDynamicSamplingContext = propagationContext.GetOrCreateDynamicSamplingContext(options);

        var secondDynamicSamplingContext = propagationContext.GetOrCreateDynamicSamplingContext(options);

        Assert.Same(firstDynamicSamplingContext, secondDynamicSamplingContext);
    }

    [Fact]
    public void CreateFromHeaders_HeadersNull_CreatesPropagationContextWithTraceAndSpanId()
    {
        var propagationContext = SentryPropagationContext.CreateFromHeaders(null, null, null);

        Assert.NotEqual(propagationContext.TraceId, SentryId.Empty);
        Assert.NotEqual(propagationContext.SpanId, SpanId.Empty);
    }

    [Fact]
    public void CreateFromHeaders_TraceHeaderNotNull_CreatesPropagationContextFromTraceHeader()
    {
        var traceHeader = new SentryTraceHeader(SentryId.Create(), SpanId.Create(), null);

        var propagationContext = SentryPropagationContext.CreateFromHeaders(null, traceHeader, null);

        Assert.Equal(traceHeader.TraceId, propagationContext.TraceId);
        Assert.NotEqual(traceHeader.SpanId, propagationContext.SpanId); // Sanity check
        Assert.Equal(traceHeader.SpanId, propagationContext.ParentSpanId);
    }

    [Fact]
    public void CreateFromHeaders_TraceHeaderNullButBaggageExists_CreatesPropagationContextWithoutDynamicSamplingContext()
    {
        var baggageHeader = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            { "sentry-sample_rate", "1.0" },
            { "sentry-trace_id", "75302ac48a024bde9a3b3734a82e36c8" },
            { "sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff" },
            { "sentry-replay_id", "bfd31b89a59d41c99d96dc2baf840ecd" }
        });

        var propagationContext = SentryPropagationContext.CreateFromHeaders(null, null, baggageHeader);

        Assert.Null(propagationContext._dynamicSamplingContext);
    }

    [Fact]
    public void CreateFromHeaders_BaggageHeaderNotNull_CreatesPropagationContextWithDynamicSamplingContext()
    {
        var traceHeader = new SentryTraceHeader(SentryId.Create(), SpanId.Create(), null);
        var baggageHeader = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            { "sentry-sample_rate", "1.0" },
            { "sentry-sample_rand", "0.1234" },
            { "sentry-trace_id", "75302ac48a024bde9a3b3734a82e36c8" },
            { "sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff" },
            { "sentry-replay_id", "bfd31b89a59d41c99d96dc2baf840ecd" }
        });

        var propagationContext = SentryPropagationContext.CreateFromHeaders(null, traceHeader, baggageHeader);

        Assert.Equal(5, propagationContext.GetOrCreateDynamicSamplingContext(new SentryOptions()).Items.Count);
    }
}
