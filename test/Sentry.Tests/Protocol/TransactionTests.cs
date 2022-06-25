using Sentry.Testing;

namespace Sentry.Tests.Protocol;

public class TransactionTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public TransactionTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        // Arrange
        var timestamp = DateTimeOffset.MaxValue;
        var context = new TransactionContext(
            SpanId.Create(),
            SpanId.Create(),
            SentryId.Create(),
            "name123",
            "op123",
            "desc",
            SpanStatus.AlreadyExists,
            null, // sampling isn't serialized and getting FluentAssertions
                  // to ignore that on Spans and contexts isn't really straight forward
            true);

        var transaction = new TransactionTracer(DisabledHub.Instance, context)
        {
            Description = "desc123",
            Status = SpanStatus.Aborted,
            User = new User { Id = "user-id" },
            Request = new Request { Method = "POST" },
            Sdk = new SdkVersion { Name = "SDK-test", Version = "1.1.1" },
            Environment = "environment",
            Level = SentryLevel.Fatal,
            Contexts =
            {
                ["context_key"] = "context_value",
                [".NET Framework"] = new Dictionary<string, string>
                {
                    [".NET Framework"] = "\"v2.0.50727\", \"v3.0\", \"v3.5\"",
                    [".NET Framework Client"] = "\"v4.8\", \"v4.0.0.0\"",
                    [".NET Framework Full"] = "\"v4.8\""
                }
            },
        };

        // Don't overwrite the contexts object as it contains trace data.
        // See https://github.com/getsentry/sentry-dotnet/issues/752

        transaction.Sdk.AddPackage(new Package("name", "version"));
        transaction.AddBreadcrumb(new Breadcrumb(timestamp, "crumb"));
        transaction.AddBreadcrumb(new Breadcrumb(
            timestamp,
            "message",
            "type",
            new Dictionary<string, string> { { "data-key", "data-value" } },
            "category",
            BreadcrumbLevel.Warning));

        transaction.SetExtra("extra_key", "extra_value");
        transaction.Fingerprint = new[] { "fingerprint" };
        transaction.SetTag("tag_key", "tag_value");

        var child1 = transaction.StartChild("child_op123", "child_desc123");
        child1.Status = SpanStatus.Unimplemented;
        child1.SetTag("q", "v");
        child1.SetExtra("f", "p");
        child1.Finish(SpanStatus.Unimplemented);

        var child2 = transaction.StartChild("child_op999", "child_desc999");
        child2.Status = SpanStatus.OutOfRange;
        child2.SetTag("xxx", "zzz");
        child2.SetExtra("f222", "p111");
        child2.Finish(SpanStatus.OutOfRange);

        transaction.Finish(SpanStatus.Aborted);

        // Act
        var finalTransaction = new Transaction(transaction);
        var actualString = finalTransaction.ToJsonString(_testOutputLogger);
        var actual = Json.Parse(actualString, Transaction.FromJson);

        // Assert
        actual.Should().BeEquivalentTo(finalTransaction, o =>
        {
            // Timestamps lose some precision when writing to JSON
            o.Using<DateTimeOffset>(ctx =>
                ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(1))
            ).WhenTypeIs<DateTimeOffset>();

            return o;
        });
    }

    [Fact]
    public void StartChild_LevelOne_Works()
    {
        // Arrange
        var transaction = new TransactionTracer(DisabledHub.Instance, "my name", "my op");

        // Act
        var child = transaction.StartChild("child op", "child desc");

        // Assert
        transaction.Spans.Should().HaveCount(1);
        transaction.Spans.Should().Contain(child);
        child.Operation.Should().Be("child op");
        child.Description.Should().Be("child desc");
        child.ParentSpanId.Should().Be(transaction.SpanId);
    }

    [Fact]
    public void StartChild_LevelTwo_Works()
    {
        // Arrange
        var transaction = new TransactionTracer(DisabledHub.Instance, "my name", "my op");

        // Act
        var child = transaction.StartChild("child op", "child desc");
        var grandChild = child.StartChild("grandchild op", "grandchild desc");

        // Assert
        transaction.Spans.Should().HaveCount(2);
        transaction.Spans.Should().Contain(child);
        transaction.Spans.Should().Contain(grandChild);
        grandChild.Operation.Should().Be("grandchild op");
        grandChild.Description.Should().Be("grandchild desc");
        grandChild.ParentSpanId.Should().Be(child.SpanId);
    }

    [Fact]
    public void StartChild_Limit_Maintained()
    {
        // Arrange
        var transaction = new TransactionTracer(DisabledHub.Instance, "my name", "my op")
        {
            IsSampled = true
        };

        // Act
        var spans = Enumerable
            .Range(0, 1000 * 2)
            .Select(i => transaction.StartChild("span " + i))
            .ToArray();

        // Assert
        transaction.Spans.Should().HaveCount(1000);
        spans.Count(s => s.IsSampled == true).Should().Be(1000);
    }

    [Fact]
    public void StartChild_SamplingInherited_Null()
    {
        // Arrange
        var transaction = new TransactionTracer(DisabledHub.Instance, "my name", "my op")
        {
            IsSampled = null
        };

        // Act
        var child = transaction.StartChild("child op", "child desc");

        // Assert
        child.IsSampled.Should().BeNull();
    }

    [Fact]
    public void StartChild_SamplingInherited_True()
    {
        // Arrange
        var transaction = new TransactionTracer(DisabledHub.Instance, "my name", "my op")
        {
            IsSampled = true
        };

        // Act
        var child = transaction.StartChild("child op", "child desc");

        // Assert
        child.IsSampled.Should().BeTrue();
    }

    [Fact]
    public void StartChild_SamplingInherited_False()
    {
        // Arrange
        var transaction = new TransactionTracer(DisabledHub.Instance, "my name", "my op")
        {
            IsSampled = false
        };

        // Act
        var child = transaction.StartChild("child op", "child desc");

        // Assert
        child.IsSampled.Should().BeFalse();
    }

    [Fact]
    public void StartChild_TraceIdInherited()
    {
        // Arrange
        var transaction = new TransactionTracer(DisabledHub.Instance, "my name", "my op");

        // Act
        var children = new[]
        {
            transaction.StartChild("op1"),
            transaction.StartChild("op2"),
            transaction.StartChild("op3")
        };

        // Assert
        children.Should().OnlyContain(s => s.TraceId == transaction.TraceId);
    }

    [Fact]
    public void Finish_RecordsTime()
    {
        // Arrange
        var transaction = new TransactionTracer(DisabledHub.Instance, "my name", "my op");

        // Act
        transaction.Finish();

        // Assert
        transaction.EndTimestamp.Should().NotBeNull();
        (transaction.EndTimestamp - transaction.StartTimestamp).Should().BeGreaterOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void Finish_UnfinishedSpansGetsFinishedWithDeadlineStatus()
    {
        // Arrange
        var transaction = new TransactionTracer(DisabledHub.Instance, "my name", "my op");
        transaction.StartChild("children1");
        transaction.StartChild("children2");
        transaction.StartChild("children3.finished").Finish(SpanStatus.Ok);
        transaction.StartChild("children4");

        // Act
        transaction.Finish();

        // Assert

        Assert.All(transaction.Spans.Where(span => !span.Operation.EndsWith("finished")), span =>
        {
            Assert.True(span.IsFinished);
            Assert.Equal(SpanStatus.DeadlineExceeded, span.Status);
        });
        Assert.Single(transaction.Spans.Where(span => span.Operation.EndsWith("finished") && span.Status == SpanStatus.Ok));
    }

    [Fact]
    public void Finish_CapturesTransaction()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var options = new SentryOptions { Dsn = DsnSamples.ValidDsnWithoutSecret };
        var hub = new Hub(options, client);

        var transaction = new TransactionTracer(hub, "my name", "my op");

        // Act
        transaction.Finish();

        // Assert
        client.Received(1).CaptureTransaction(Arg.Any<Transaction>());
    }

    [Fact]
    public void Finish_LinksExceptionToEvent()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var options = new SentryOptions { Dsn = DsnSamples.ValidDsnWithoutSecret };
        var hub = new Hub(options, client);

        var exception = new InvalidOperationException();
        var transaction = new TransactionTracer(hub, "my name", "my op");

        // Act
        transaction.Finish(exception);

        var @event = new SentryEvent(exception);
        hub.CaptureEvent(@event);

        // Assert
        transaction.Status.Should().Be(SpanStatus.InternalError);

        client.Received(1).CaptureEvent(Arg.Is<SentryEvent>(e =>
            e.Contexts.Trace.TraceId == transaction.TraceId &&
            e.Contexts.Trace.SpanId == transaction.SpanId &&
            e.Contexts.Trace.ParentSpanId == transaction.ParentSpanId
        ), Arg.Any<Scope>());
    }

    [Fact]
    public void Finish_NoStatus_DefaultsToUnknown()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var transaction = new TransactionTracer(hub, "my name", "my op");

        // Act
        transaction.Finish();

        // Assert
        transaction.Status.Should().Be(SpanStatus.UnknownError);
    }

    [Fact]
    public void Finish_StatusSet_DoesNotOverride()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var transaction = new TransactionTracer(hub, "my name", "my op")
        {
            Status = SpanStatus.DataLoss
        };

        // Act
        transaction.Finish();

        // Assert
        transaction.Status.Should().Be(SpanStatus.DataLoss);
    }
}
