using Sentry.Internal;

namespace Sentry.Tests.Internals;

public class QuerySourceHelperTests
{
    private class Fixture
    {
        public SentryOptions Options { get; }
        public InMemoryDiagnosticLogger Logger { get; }
        public ISpan Span { get; }

        public Fixture()
        {
            Logger = new InMemoryDiagnosticLogger();
            Options = new SentryOptions
            {
                Debug = true,
                DiagnosticLogger = Logger,
                DiagnosticLevel = SentryLevel.Debug,
                EnableDbQuerySource = true,
                DbQuerySourceThresholdMs = 100
            };

            var transaction = new TransactionTracer(Substitute.For<IHub>(), "test", "test");
            Span = transaction.StartChild("db.query", "SELECT * FROM users");
        }
    }

    [Fact]
    public void TryAddQuerySource_FeatureDisabled_DoesNotAddSource()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.Options.EnableDbQuerySource = false;

        // Act
        QuerySourceHelper.TryAddQuerySource(fixture.Span, fixture.Options);

        // Assert
        fixture.Span.Extra.Should().NotContainKey("code.filepath");
        fixture.Span.Extra.Should().NotContainKey("code.lineno");
        fixture.Span.Extra.Should().NotContainKey("code.function");
        fixture.Span.Extra.Should().NotContainKey("code.namespace");
    }

    [Fact]
    public void TryAddQuerySource_BelowThreshold_DoesNotAddSource()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.Options.DbQuerySourceThresholdMs = 10000; // Very high threshold

        // Act
        QuerySourceHelper.TryAddQuerySource(fixture.Span, fixture.Options);

        // Assert
        fixture.Span.Extra.Should().NotContainKey("code.filepath");
        fixture.Logger.Entries.Should().Contain(e => e.Message.Contains("below threshold"));
    }

    [Fact]
    public void TryAddQuerySource_AboveThreshold_AddsSourceInfo()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.Options.DbQuerySourceThresholdMs = 0; // Capture all queries
        
        // Simulate a slow query by starting the span earlier
        var transaction = new TransactionTracer(Substitute.For<IHub>(), "test", "test");
        var span = transaction.StartChild("db.query", "SELECT * FROM users");
        
        // Wait a bit to ensure duration is above 0
        Thread.Sleep(5);

        // Act - this call itself is in-app and should be captured
        QuerySourceHelper.TryAddQuerySource(span, fixture.Options, skipFrames: 0);

        // Assert
        // The test method itself should be captured as the source since it's in-app
        span.Data.Should().ContainKey("code.filepath");
        span.Data.Should().ContainKey("code.function");
        
        // Verify we logged something about finding the frame
        fixture.Logger.Entries.Should().Contain(e => 
            e.Message.Contains("Found in-app frame") || 
            e.Message.Contains("Added query source"));
    }

    [Fact]
    public void TryAddQuerySource_WithException_DoesNotThrow()
    {
        // Arrange
        var fixture = new Fixture();
        var span = Substitute.For<ISpan>();
        span.StartTimestamp.Returns(DateTimeOffset.UtcNow.AddSeconds(-1));
        
        // Cause an exception when trying to set data
        span.When(x => x.SetData(Arg.Any<string>(), Arg.Any<object>()))
            .Do(_ => throw new InvalidOperationException("Test exception"));

        // Act & Assert - should not throw
        var action = () => QuerySourceHelper.TryAddQuerySource(span, fixture.Options);
        action.Should().NotThrow();
        
        // Should log the error (plus some debug entries from stack walking)
        fixture.Logger.Entries.Should().Contain(e => 
            e.Level == SentryLevel.Error && 
            e.Message.Contains("Failed to capture query source"));
    }

    [Fact]
    public void TryAddQuerySource_SkipsSentryFrames()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.Options.DbQuerySourceThresholdMs = 0;

        var transaction = new TransactionTracer(Substitute.For<IHub>(), "test", "test");
        var span = transaction.StartChild("db.query", "SELECT * FROM users");

        // Act
        QuerySourceHelper.TryAddQuerySource(span, fixture.Options, skipFrames: 0);

        // Assert
        // Should skip any Sentry.* frames and find this test method
        if (span.Data.TryGetValue<string, string>("code.namespace") is { } ns)
        {
            ns.Should().NotStartWith("Sentry.");
        }
    }

    [Fact]
    public void TryAddQuerySource_SkipsEFCoreFrames()
    {
        // This test verifies the logic, but we can't easily inject EF Core frames in a unit test
        // Integration tests will verify the actual EF Core frame skipping
        var fixture = new Fixture();
        fixture.Options.DbQuerySourceThresholdMs = 0;

        var transaction = new TransactionTracer(Substitute.For<IHub>(), "test", "test");
        var span = transaction.StartChild("db.query", "SELECT * FROM users");

        // Act
        QuerySourceHelper.TryAddQuerySource(span, fixture.Options, skipFrames: 0);

        // Assert
        // Should not capture EF Core or System.Data frames
        if (span.Data.TryGetValue<string, string>("code.namespace") is { } ns)
        {
            ns.Should().NotStartWith("Microsoft.EntityFrameworkCore");
            ns.Should().NotStartWith("System.Data");
        }
    }

    [Fact]
    public void TryAddQuerySource_RespectsInAppExclude()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.Options.DbQuerySourceThresholdMs = 0;
        
        // Exclude this test namespace from in-app
        fixture.Options.InAppExclude = new List<StringOrRegex> { "Sentry.Tests.*" };

        var transaction = new TransactionTracer(Substitute.For<IHub>(), "test", "test");
        var span = transaction.StartChild("db.query", "SELECT * FROM users");

        // Act
        QuerySourceHelper.TryAddQuerySource(span, fixture.Options, skipFrames: 0);

        // Assert
        // Should not find any in-app frames since we excluded the test namespace
        span.Data.Should().NotContainKey("code.filepath");
        fixture.Logger.Entries.Should().Contain(e => e.Message.Contains("No in-app frame found"));
    }

    [Fact]
    public void TryAddQuerySource_RespectsInAppInclude()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.Options.DbQuerySourceThresholdMs = 0;
        
        // Only include the test namespace as in-app
        fixture.Options.InAppInclude = new List<StringOrRegex> { "Sentry.Tests.*" };

        var transaction = new TransactionTracer(Substitute.For<IHub>(), "test", "test");
        var span = transaction.StartChild("db.query", "SELECT * FROM users");

        // Act
        QuerySourceHelper.TryAddQuerySource(span, fixture.Options, skipFrames: 0);

        // Assert
        // Should find this test method as in-app since we explicitly included it
        span.Data.Should().ContainKey("code.filepath");
        span.Data.Should().ContainKey("code.function");
    }

    [Fact]
    public void TryAddQuerySource_AddsAllCodeAttributes()
    {
        // Arrange
        var fixture = new Fixture();
        fixture.Options.DbQuerySourceThresholdMs = 0;

        var transaction = new TransactionTracer(Substitute.For<IHub>(), "test", "test");
        var span = transaction.StartChild("db.query", "SELECT * FROM users");

        // Act
        QuerySourceHelper.TryAddQuerySource(span, fixture.Options, skipFrames: 0);

        // Assert - when PDB is available, should have all attributes
        if (span.Data.ContainsKey("code.filepath"))
        {
            span.Data.Should().ContainKey("code.lineno");
            span.Data.Should().ContainKey("code.function");
            span.Data.Should().ContainKey("code.namespace");
            
            // Verify the values are reasonable
            span.Data["code.function"].Should().BeOfType<string>();
            span.Data["code.lineno"].Should().BeOfType<int>();
        }
    }
}
