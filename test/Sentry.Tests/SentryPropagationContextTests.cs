namespace Sentry.Tests;

public class SentryPropagationContextTests
{
    private class Fixture
    {
        public SentryId ActiveReplayId { get; } = SentryId.Create();
        public IReplaySession InactiveReplaySession { get; }
        public IReplaySession ActiveReplaySession { get; }

        public Fixture()
        {
            ActiveReplaySession = Substitute.For<IReplaySession>();
            ActiveReplaySession.ActiveReplayId.Returns(ActiveReplayId);

            InactiveReplaySession = Substitute.For<IReplaySession>();
            InactiveReplaySession.ActiveReplayId.Returns((SentryId?)null);
        }
    }

    private readonly Fixture _fixture = new();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CopyConstructor_CreatesCopyWithReplayId(bool replaySessionIsActive)
    {
        var original = new SentryPropagationContext();
        original.GetOrCreateDynamicSamplingContext(new SentryOptions { Dsn = ValidDsn }, _fixture.InactiveReplaySession);

        var copy = new SentryPropagationContext(original);

        Assert.Equal(original.TraceId, copy.TraceId);
        Assert.Equal(original.SpanId, copy.SpanId);
        Assert.Equal(original._dynamicSamplingContext!.Items.Count, copy._dynamicSamplingContext!.Items.Count);
        foreach (var dscItem in original._dynamicSamplingContext!.Items)
        {
            if (dscItem.Key == "replay_id")
            {
                copy._dynamicSamplingContext!.Items["replay_id"].Should().Be(replaySessionIsActive
                    // We overwrite the replay_id when we have an active replay session
                    ? _fixture.ActiveReplayId.ToString()
                    // Otherwise we propagate whatever was in the baggage header
                    : dscItem.Value);
            }
            else
            {
                copy._dynamicSamplingContext!.Items.Should()
                    .Contain(kvp => kvp.Key == dscItem.Key && kvp.Value == dscItem.Value);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetOrCreateDynamicSamplingContext_DynamicSamplingContextIsNull_CreatesDynamicSamplingContext(bool replaySessionIsActive)
    {
        var options = new SentryOptions { Dsn = ValidDsn };
        var propagationContext = new SentryPropagationContext();

        Assert.Null(propagationContext._dynamicSamplingContext); // Sanity check
        _ = propagationContext.GetOrCreateDynamicSamplingContext(options, replaySessionIsActive ? _fixture.ActiveReplaySession : _fixture.InactiveReplaySession);

        Assert.NotNull(propagationContext._dynamicSamplingContext);
        if (replaySessionIsActive)
        {
            // We add the replay_id automatically when we have an active replay session
            Assert.Equal(_fixture.ActiveReplayId.ToString(), Assert.Contains("replay_id", propagationContext._dynamicSamplingContext.Items));
        }
        else
        {
            Assert.DoesNotContain("replay_id", propagationContext._dynamicSamplingContext.Items);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetOrCreateDynamicSamplingContext_DynamicSamplingContextIsNotNull_ReturnsSameDynamicSamplingContext(bool replaySessionIsActive)
    {
        var options = new SentryOptions { Dsn = ValidDsn };
        var propagationContext = new SentryPropagationContext();
        var firstDynamicSamplingContext = propagationContext.GetOrCreateDynamicSamplingContext(options, replaySessionIsActive ? _fixture.ActiveReplaySession : _fixture.InactiveReplaySession);

        var secondDynamicSamplingContext = propagationContext.GetOrCreateDynamicSamplingContext(options, replaySessionIsActive ? _fixture.ActiveReplaySession : _fixture.InactiveReplaySession);

        Assert.Same(firstDynamicSamplingContext, secondDynamicSamplingContext);
    }

    [Fact]
    public void CreateFromHeaders_HeadersNull_CreatesPropagationContextWithTraceAndSpanAndReplayId()
    {
        var propagationContext = SentryPropagationContext.CreateFromHeaders(null, null, null, _fixture.ActiveReplaySession);

        Assert.NotEqual(propagationContext.TraceId, SentryId.Empty);
        Assert.NotEqual(propagationContext.SpanId, SpanId.Empty);
        Assert.Null(propagationContext._dynamicSamplingContext);
    }

    [Fact]
    public void CreateFromHeaders_TraceHeaderNotNull_CreatesPropagationContextFromTraceHeader()
    {
        var traceHeader = new SentryTraceHeader(SentryId.Create(), SpanId.Create(), null);

        var propagationContext = SentryPropagationContext.CreateFromHeaders(null, traceHeader, null, _fixture.ActiveReplaySession);

        Assert.Equal(traceHeader.TraceId, propagationContext.TraceId);
        Assert.NotEqual(traceHeader.SpanId, propagationContext.SpanId); // Sanity check
        Assert.Equal(traceHeader.SpanId, propagationContext.ParentSpanId);
        Assert.Null(propagationContext._dynamicSamplingContext);
    }

    [Fact]
    public void CreateFromHeaders_BaggageExistsButTraceHeaderNull_CreatesPropagationContextWithoutDynamicSamplingContext()
    {
        var baggageHeader = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            { "sentry-sample_rate", "1.0" },
            { "sentry-trace_id", "75302ac48a024bde9a3b3734a82e36c8" },
            { "sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff" },
            { "sentry-replay_id", "bfd31b89a59d41c99d96dc2baf840ecd" }
        });

        var propagationContext = SentryPropagationContext.CreateFromHeaders(null, null, baggageHeader, _fixture.InactiveReplaySession);

        Assert.Null(propagationContext._dynamicSamplingContext);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateFromHeaders_BaggageHeaderNotNull_CreatesPropagationContextWithDynamicSamplingContext(bool replaySessionIsActive)
    {
        // Arrange
        var traceHeader = new SentryTraceHeader(SentryId.Create(), SpanId.Create(), null);
        var baggageHeader = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            { "sentry-sample_rate", "1.0" },
            { "sentry-sample_rand", "0.1234" },
            { "sentry-trace_id", "75302ac48a024bde9a3b3734a82e36c8" },
            { "sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff" },
            { "sentry-replay_id", "bfd31b89a59d41c99d96dc2baf840ecd" }
        });
        var replaySession = replaySessionIsActive ? _fixture.ActiveReplaySession : _fixture.InactiveReplaySession;

        // Act
        var propagationContext = SentryPropagationContext.CreateFromHeaders(null, traceHeader, baggageHeader, replaySession);

        // Assert
        var dsc = propagationContext.GetOrCreateDynamicSamplingContext(new SentryOptions(), replaySession);
        Assert.Equal(5, dsc.Items.Count);
        if (replaySessionIsActive)
        {
            // We add the replay_id automatically when we have an active replay session
            Assert.Equal(_fixture.ActiveReplayId.ToString(), Assert.Contains("replay_id", dsc.Items));
        }
        else
        {
            // Otherwise we inherit the replay_id from the baggage header
            Assert.Equal("bfd31b89a59d41c99d96dc2baf840ecd", Assert.Contains("replay_id", dsc.Items));
        }
    }

    // Decision matrix tests for ShouldContinueTrace

    [Theory]
    // strict=false cases
    [InlineData("1", "1", false, true)]   // matching orgs → continue
    [InlineData(null, "1", false, true)]   // baggage missing → continue
    [InlineData("1", null, false, true)]   // SDK missing → continue
    [InlineData(null, null, false, true)]  // both missing → continue
    [InlineData("1", "2", false, false)]   // mismatch → new trace
    // strict=true cases
    [InlineData("1", "1", true, true)]     // matching orgs → continue
    [InlineData(null, "1", true, false)]   // baggage missing → new trace
    [InlineData("1", null, true, false)]   // SDK missing → new trace
    [InlineData(null, null, true, true)]   // both missing → continue
    [InlineData("1", "2", true, false)]    // mismatch → new trace
    public void ShouldContinueTrace_DecisionMatrix(string baggageOrgId, string sdkOrgId, bool strict, bool expected)
    {
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            StrictTraceContinuation = strict
        };
        if (sdkOrgId != null)
        {
            options.OrgId = sdkOrgId;
        }

        BaggageHeader baggage = null;
        if (baggageOrgId != null)
        {
            baggage = BaggageHeader.TryParse($"sentry-trace_id=bc6d53f15eb88f4320054569b8c553d4,sentry-org_id={baggageOrgId}");
        }
        else
        {
            baggage = BaggageHeader.TryParse("sentry-trace_id=bc6d53f15eb88f4320054569b8c553d4");
        }

        var result = SentryPropagationContext.ShouldContinueTrace(options, baggage);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CreateFromHeaders_WithOrgMismatch_StartsNewTrace()
    {
        var options = new SentryOptions
        {
            Dsn = "https://key@o2.ingest.sentry.io/456",
            StrictTraceContinuation = false
        };

        var traceHeader = SentryTraceHeader.Parse("bc6d53f15eb88f4320054569b8c553d4-b72fa28504b07285-1");
        var baggage = BaggageHeader.TryParse("sentry-trace_id=bc6d53f15eb88f4320054569b8c553d4,sentry-org_id=1");

        var propagationContext = SentryPropagationContext.CreateFromHeaders(null, traceHeader, baggage, _fixture.InactiveReplaySession, options);
        Assert.NotEqual(SentryId.Parse("bc6d53f15eb88f4320054569b8c553d4"), propagationContext.TraceId);
    }

    [Fact]
    public void CreateFromHeaders_WithOrgMatch_ContinuesTrace()
    {
        var options = new SentryOptions
        {
            Dsn = "https://key@o1.ingest.sentry.io/456",
            StrictTraceContinuation = false
        };

        var traceHeader = SentryTraceHeader.Parse("bc6d53f15eb88f4320054569b8c553d4-b72fa28504b07285-1");
        var baggage = BaggageHeader.TryParse("sentry-trace_id=bc6d53f15eb88f4320054569b8c553d4,sentry-org_id=1,sentry-public_key=key,sentry-sample_rate=1.0,sentry-sample_rand=0.5000,sentry-sampled=true");

        var propagationContext = SentryPropagationContext.CreateFromHeaders(null, traceHeader, baggage, _fixture.InactiveReplaySession, options);
        Assert.Equal(SentryId.Parse("bc6d53f15eb88f4320054569b8c553d4"), propagationContext.TraceId);
    }
}
