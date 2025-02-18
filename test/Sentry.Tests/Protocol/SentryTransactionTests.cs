namespace Sentry.Tests.Protocol;

public class SentryTransactionTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public SentryTransactionTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void NewTransactionTracer_ConstructingWithNameAndOperation_HasValidStartTime()
    {
        var actualTransaction = new TransactionTracer(DisabledHub.Instance, "test-name", "test-operation");

        Assert.NotEqual(DateTimeOffset.MinValue, actualTransaction.StartTimestamp);
    }

    [Fact]
    public void NewTransactionTracer_ConstructingWithContext_HasValidStartTime()
    {
        var context = new TransactionContext("test-name", "test-operation");

        var actualTransaction = new TransactionTracer(DisabledHub.Instance, context);

        Assert.NotEqual(DateTimeOffset.MinValue, actualTransaction.StartTimestamp);
    }

    [Fact]
    public async Task NewTransactionTracer_IdleTimeoutProvided_AutomaticallyFinishes()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            Debug = true
        };
        var hub = new Hub(options, client);
        var context = new TransactionContext("my name",
            "my operation",
            SpanId.Create(),
            SpanId.Create(),
            SentryId.Create(),
            "description",
            SpanStatus.Ok, null, true, TransactionNameSource.Component);

        var transaction = new TransactionTracer(hub, context, TimeSpan.FromMilliseconds(2));

        // Act
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert
        transaction.IsFinished.Should().BeTrue();
    }

    [Fact]
    public void Redact_Redacts_Urls()
    {
        // Arrange
        var timestamp = DateTimeOffset.MaxValue;
        var name = "name123 https://user@not.redacted";
        var operation = "op123 https://user@not.redacted";
        var description = "desc123 https://user@sentry.io"; // should be redacted
        var platform = "platform123 https://user@not.redacted";
        var release = "release123 https://user@not.redacted";
        var distribution = "distribution123 https://user@not.redacted";
        var environment = "environment123 https://user@not.redacted";
        var breadcrumbMessage = "message https://user@sentry.io"; // should be redacted
        var breadcrumbDataValue = "data-value https://user@sentry.io"; // should be redacted
        var tagValue = "tag_value https://user@not.redacted";
        var context = new TransactionContext(name,
            operation,
            SpanId.Create(),
            SpanId.Create(),
            SentryId.Create(),
            description,
            SpanStatus.AlreadyExists, null, true, TransactionNameSource.Component);

        var txTracer = new TransactionTracer(DisabledHub.Instance, context)
        {
            Name = name,
            Operation = operation,
            Description = description,
            Platform = platform,
            Release = release,
            Distribution = distribution,
            Status = SpanStatus.Aborted,
            // We don't redact the User or the Request since, if SendDefaultPii is false, we don't add these to the
            // transaction in the SDK anyway (by default they don't get sent... but the user can always override this
            // behavior if they need)
            User = new SentryUser { Id = "user-id", Username = "username", Email = "bob@foo.com", IpAddress = "127.0.0.1" },
            Request = new SentryRequest { Method = "POST", Url = "https://user@not.redacted" },
            Sdk = new SdkVersion { Name = "SDK-test", Version = "1.1.1" },
            Environment = environment,
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
            }
        };

        txTracer.Sdk.AddPackage(new SentryPackage("name", "version"));
        txTracer.AddBreadcrumb(new Breadcrumb(timestamp, breadcrumbMessage));
        txTracer.AddBreadcrumb(new Breadcrumb(
            timestamp,
            "message",
            "type",
            new Dictionary<string, string> { { "data-key", breadcrumbDataValue } },
            "category",
            BreadcrumbLevel.Warning));
        txTracer.SetTag("tag_key", tagValue);

        var child1 = txTracer.StartChild("child_op123", "child_desc123 https://user@sentry.io");
        child1.Status = SpanStatus.Unimplemented;
        child1.SetTag("q", "v");
        child1.SetExtra("f", "p");
        child1.Finish(SpanStatus.Unimplemented);

        var child2 = txTracer.StartChild("child_op999", "child_desc999 https://user:password@sentry.io");
        child2.Status = SpanStatus.OutOfRange;
        child2.SetTag("xxx", "zzz");
        child2.SetExtra("f222", "p111");
        child2.Finish(SpanStatus.OutOfRange);

        // Don't finish the tracer - that would cause the spans to be released
        // txTracer.Finish(SpanStatus.Aborted);

        // Act
        var transaction = new SentryTransaction(txTracer);
        transaction.Redact();

        // Assert
        using (new AssertionScope())
        {
            transaction.Name.Should().Be(name);
            transaction.Operation.Should().Be(operation);
            transaction.Description.Should().Be($"desc123 https://{PiiExtensions.RedactedText}@sentry.io");
            transaction.Platform.Should().Be(platform);
            transaction.Release.Should().Be(release);
            transaction.Distribution.Should().Be(distribution);
            transaction.Environment.Should().Be(environment);
            var breadcrumbs = transaction.Breadcrumbs.ToArray();
            breadcrumbs.Length.Should().Be(2);
            breadcrumbs.Should().Contain(b => b.Message == $"message https://{PiiExtensions.RedactedText}@sentry.io");
            breadcrumbs.Should().Contain(b => b.Data != null && b.Data["data-key"] == $"data-value https://{PiiExtensions.RedactedText}@sentry.io");
            var spans = transaction.Spans.ToArray();
            spans.Should().Contain(s => s.Operation == "child_op123" && s.Description == $"child_desc123 https://{PiiExtensions.RedactedText}@sentry.io");
            spans.Should().Contain(s => s.Operation == "child_op999" && s.Description == $"child_desc999 https://{PiiExtensions.RedactedText}:{PiiExtensions.RedactedText}@sentry.io");
            transaction.Tags["tag_key"].Should().Be(tagValue);
        }
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        // Arrange
        var timestamp = DateTimeOffset.MaxValue;
        var context = new TransactionContext("name123",
            "op123",
            SpanId.Create(),
            SpanId.Create(),
            SentryId.Create(), // sampling isn't serialized and getting FluentAssertions
                               // to ignore that on Spans and contexts isn't really straight forward
            "desc",
            SpanStatus.AlreadyExists, null, true, TransactionNameSource.Component);
        context.Origin = "auto.serialize.transaction";

        var transaction = new TransactionTracer(DisabledHub.Instance, context)
        {
            Description = "desc123",
            Status = SpanStatus.Aborted,
            User = new SentryUser { Id = "user-id" },
            Request = new SentryRequest { Method = "POST" },
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
            }
        };

        // Don't overwrite the contexts object as it contains trace data.
        // See https://github.com/getsentry/sentry-dotnet/issues/752

        transaction.Sdk.AddPackage(new SentryPackage("name", "version"));
        transaction.AddBreadcrumb(new Breadcrumb(timestamp, "crumb"));
        transaction.AddBreadcrumb(new Breadcrumb(
            timestamp,
            "message",
            "type",
            new Dictionary<string, string> { { "data-key", "data-value" } },
            "category",
            BreadcrumbLevel.Warning));

        transaction.SetData("extra_key", "extra_value");
        transaction.Fingerprint = new[] { "fingerprint" };
        transaction.SetTag("tag_key", "tag_value");
        transaction.SetMeasurement("measurement_1", 111);
        transaction.SetMeasurement("measurement_2", 2.34, MeasurementUnit.Custom("things"));
        transaction.SetMeasurement("measurement_3", 333, MeasurementUnit.Information.Terabyte);
        transaction.SetMeasurement("measurement_4", 0, MeasurementUnit.None);

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
        var finalTransaction = new SentryTransaction(transaction);
        var actualString = finalTransaction.ToJsonString(_testOutputLogger);
        var actual = Json.Parse(actualString, SentryTransaction.FromJson);

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
    public void SerializeObject_TransactionContainsUnfinishedSpan_SerializesDeserializesValidObject()
    {
        // Arrange
        SentryTransaction capturedTransaction = null;
        var hub = Substitute.For<IHub>();
        hub.CaptureTransaction(Arg.Do<SentryTransaction>(t => capturedTransaction = t));

        var transaction = new TransactionTracer(hub, "test.name", "test.operation");
        transaction.StartChild("child_op123", "child_desc123");
        transaction.Finish(SpanStatus.Aborted);

        // Act
        var actualString = capturedTransaction.ToJsonString(_testOutputLogger);
        var actualTransaction = Json.Parse(actualString, SentryTransaction.FromJson);

        // Assert
        Assert.Single(actualTransaction.Spans); // Sanity Check
        Assert.Null(actualTransaction.Spans.First().EndTimestamp);
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
    public void Finish_SentryRequestSpansGetIgnored()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var transactionTracer = new TransactionTracer(hub, "my name", "my op");
        transactionTracer.StartChild("normalRequest").Finish(SpanStatus.Ok);
        var sentryRequest = (SpanTracer)transactionTracer.StartChild("sentryRequest");
        sentryRequest.IsSentryRequest = true;

        SentryTransaction transaction = null;
        hub.CaptureTransaction(Arg.Do<Sentry.SentryTransaction>(t => transaction = t));

        // Act
        transactionTracer.Finish();

        // Assert
        transaction.Should().NotBeNull();
        transaction.Spans.Should().Contain(s => s.Operation == "normalRequest");
        transaction.Spans.Should().NotContain(s => s.Operation == "sentryRequest");
    }

    [SkippableFact]
    public async Task Finish_SentryRequestTransactionGetsIgnored()
    {
        // See https://github.com/getsentry/sentry-dotnet/issues/2785
        Skip.If(TestEnvironment.IsGitHubActions, "This test is flaky in CI");

        // Arrange
        var client = Substitute.For<ISentryClient>();
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
        };
        var hub = new Hub(options, client);
        var context = new TransactionContext("my name",
            "my operation",
            SpanId.Create(),
            SpanId.Create(),
            SentryId.Create(),
            "description",
            SpanStatus.Ok, null, true, TransactionNameSource.Component);

        var transaction = new TransactionTracer(hub, context, TimeSpan.FromMilliseconds(2))
        {
            IsSentryRequest = true
        };

        // Act
        await Task.Delay(TimeSpan.FromMilliseconds(5));

        // Assert
        transaction.IsFinished.Should().BeFalse();
    }

    [Fact]
    public void Finish_CapturesTransaction()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var options = new SentryOptions { Dsn = ValidDsn };
        var hub = new Hub(options, client);

        var transaction = new TransactionTracer(hub, "my name", "my op");

        // Act
        transaction.Finish();

        // Assert
        client.Received(1).CaptureTransaction(Arg.Any<SentryTransaction>(), Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    [Fact]
    public void Finish_LinksExceptionToEvent()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var options = new SentryOptions { Dsn = ValidDsn };
        var hub = new Hub(options, client);

        var exception = new InvalidOperationException();
        var transaction = new TransactionTracer(hub, "my name", "my op");

        // Act
        transaction.Finish(exception);

        var @event = new SentryEvent(exception);
        hub.CaptureEvent(@event);

        // Assert
        transaction.Status.Should().Be(SpanStatus.InternalError);

        client.Received(1).CaptureEvent(
            Arg.Is<SentryEvent>(e =>
                e.Contexts.Trace.TraceId == transaction.TraceId &&
                e.Contexts.Trace.SpanId == transaction.SpanId &&
                e.Contexts.Trace.ParentSpanId == transaction.ParentSpanId
            ),
            Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    [Fact]
    public void Finish_NoStatus_DefaultsToOk()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var transaction = new TransactionTracer(hub, "my name", "my op");

        // Act
        transaction.Finish();

        // Assert
        transaction.Status.Should().Be(SpanStatus.Ok);
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

    [Fact]
    public void Finish_ChildSpan_NoStatus_DefaultsToOk()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var transaction = new TransactionTracer(hub, "my name", "my op");
        var span = transaction.StartChild("child op");

        // Act
        span.Finish();

        // Assert
        span.Status.Should().Be(SpanStatus.Ok);
    }

    [Fact]
    public void Finish_ChildSpan_StatusSet_DoesNotOverride()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var transaction = new TransactionTracer(hub, "my name", "my op");
        var span = transaction.StartChild("child op");
        span.Status = SpanStatus.DataLoss;

        // Act
        span.Finish();

        // Assert
        span.Status.Should().Be(SpanStatus.DataLoss);
    }

    [Fact]
    public void ISpan_GetTransaction_FromTransaction()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        ISpan transaction = new TransactionTracer(hub, "my name", "my op");

        // Act
        var result = transaction.GetTransaction();

        // Assert
        Assert.Same(transaction, result);
    }

    [Fact]
    public void ISpan_GetTransaction_FromSpan()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var transaction = new TransactionTracer(hub, "my name", "my op");
        var span = transaction.StartChild("child op");

        // Act
        var result = span.GetTransaction();

        // Assert
        Assert.Same(transaction, result);
    }

    [Fact]
    public void FromTracerSpans_Filters_SentryRequests()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var context = new TransactionContext("name", "op")
        {
            Instrumenter = Instrumenter.OpenTelemetry
        };
        var tracer = new TransactionTracer(hub, context);
        var span1 = (SpanTracer)tracer.StartChild(null, tracer.SpanId, "span1", Instrumenter.OpenTelemetry);
        var span2 = (SpanTracer)tracer.StartChild(null, span1.SpanId, "span2", Instrumenter.OpenTelemetry);
        span2.IsSentryRequest = true;
        var span3 = (SpanTracer)tracer.StartChild(null, span2.SpanId, "span3", Instrumenter.OpenTelemetry);

        // Act
        var transaction = new SentryTransaction(tracer);

        // Assert
        var spans = transaction.Spans.ToArray();
        spans.Length.Should().Be(2);
        spans.Should().Contain(x => x.SpanId == span1.SpanId);
        spans.Should().Contain(x => x.SpanId == span3.SpanId);
    }

    [Fact]
    public void FromTracerSpans_OtelInstrumentation_FilteredSpansRemoved()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var context = new TransactionContext("name", "op")
        {
            Instrumenter = Instrumenter.OpenTelemetry
        };
        var tracer = new TransactionTracer(hub, context);
        var span1 = (SpanTracer)tracer.StartChild(null, tracer.SpanId, "span1", Instrumenter.OpenTelemetry);
        var span2 = (SpanTracer)tracer.StartChild(null, span1.SpanId, "span2", Instrumenter.OpenTelemetry);
        span2.IsFiltered = () => true;
        var span3 = (SpanTracer)tracer.StartChild(null, span2.SpanId, "span3", Instrumenter.OpenTelemetry);
        span3.IsFiltered = () => true;
        var span4 = (SpanTracer)tracer.StartChild(null, span3.SpanId, "span4", Instrumenter.OpenTelemetry);

        // Act
        var transaction = new SentryTransaction(tracer);

        // Assert
        var spans = transaction.Spans.ToArray();
        spans.Length.Should().Be(2);
        spans.Should().Contain(x => x.SpanId == span1.SpanId);
        var lastSpan = spans.SingleOrDefault(x => x.SpanId == span4.SpanId);
        lastSpan.Should().NotBeNull();
        lastSpan!.ParentSpanId.Should().Be(span1.SpanId);
    }

    [Fact]
    public void FromTracerSpans_SentryInstrumentation_FilteredSpansRemain()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var context = new TransactionContext("name", "op")
        {
            Instrumenter = Instrumenter.Sentry
        };
        var tracer = new TransactionTracer(hub, context);
        var span1 = (SpanTracer)tracer.StartChild(null, tracer.SpanId, "span1", Instrumenter.OpenTelemetry);
        var span2 = (SpanTracer)tracer.StartChild(null, span1.SpanId, "span2", Instrumenter.OpenTelemetry);
        span2.IsFiltered = () => true;
        var span3 = (SpanTracer)tracer.StartChild(null, span2.SpanId, "span3", Instrumenter.OpenTelemetry);
        span3.IsFiltered = () => true;
        tracer.StartChild(null, span3.SpanId, "span4", Instrumenter.OpenTelemetry);

        // Act
        var transaction = new SentryTransaction(tracer);

        // Assert
        transaction.Spans.Count.Should().Be(4);
    }
}
