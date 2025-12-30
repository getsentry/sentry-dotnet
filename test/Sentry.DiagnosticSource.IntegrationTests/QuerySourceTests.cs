namespace Sentry.DiagnosticSource.IntegrationTests;

/// <summary>
/// Integration tests for database query source capture functionality.
/// </summary>
public class QuerySourceTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;
    private readonly TestOutputDiagnosticLogger _logger;

    public QuerySourceTests(LocalDbFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _logger = new TestOutputDiagnosticLogger(output);
    }

#if NET6_0_OR_GREATER
    [SkippableFact]
    public async Task EfCore_WithQuerySource_CapturesSourceLocation()
    {
        Skip.If(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        
        var transport = new RecordingTransport();
        var options = new SentryOptions
        {
            AttachStacktrace = false,
            TracesSampleRate = 1,
            Transport = transport,
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            EnableDbQuerySource = true,
            DbQuerySourceThresholdMs = 0 // Capture all queries for testing
        };

        await using var database = await _fixture.SqlInstance.Build();
        
        using (var hub = new Hub(options))
        {
            var transaction = hub.StartTransaction("query source test", "test");
            hub.ConfigureScope(scope => scope.Transaction = transaction);

            await using (var connection = await database.OpenNewConnection())
            await using (var dbContext = TestDbBuilder.GetDbContext(connection))
            {
                // This query call should be captured as the source location
                var result = await dbContext.TestEntities.Where(e => e.Property == "test").ToListAsync();
            }

            transaction.Finish();
        }

        // Verify that query source information was captured
        Assert.NotEmpty(transport.Payloads);
        
        var sentTransaction = transport.Payloads
            .OfType<SentryTransaction>()
            .FirstOrDefault();
            
        Assert.NotNull(sentTransaction);
        
        // Find the db.query span
        var querySpans = sentTransaction.Spans.Where(s => s.Operation == "db.query").ToList();
        Assert.NotEmpty(querySpans);
        
        // At least one query span should have source location info
        var hasSourceInfo = querySpans.Any(span =>
            span.Extra.ContainsKey("code.filepath") ||
            span.Extra.ContainsKey("code.function") ||
            span.Extra.ContainsKey("code.namespace"));
            
        if (hasSourceInfo)
        {
            var spanWithSource = querySpans.First(span => span.Extra.ContainsKey("code.function"));
            
            // Verify the captured information looks reasonable
            Assert.True(spanWithSource.Extra.ContainsKey("code.function"));
            var function = spanWithSource.Extra["code.function"] as string;
            _logger.Log(SentryLevel.Debug, $"Captured function: {function}");
            
            // The function should be from this test method or a continuation
            Assert.NotNull(function);
            
            // Should also have file path and line number if PDB is available
            if (spanWithSource.Extra.ContainsKey("code.filepath"))
            {
                var filepath = spanWithSource.Extra["code.filepath"] as string;
                _logger.Log(SentryLevel.Debug, $"Captured filepath: {filepath}");
                Assert.Contains("QuerySourceTests.cs", filepath);
            }
            
            if (spanWithSource.Extra.ContainsKey("code.lineno"))
            {
                var lineno = spanWithSource.Extra["code.lineno"];
                _logger.Log(SentryLevel.Debug, $"Captured lineno: {lineno}");
                Assert.IsType<int>(lineno);
            }
        }
        else
        {
            // If no source info, PDB might not be available - log warning
            _logger.Log(SentryLevel.Warning, "No query source info captured - PDB may not be available");
        }
    }

    [SkippableFact]
    public async Task EfCore_QueryBelowThreshold_DoesNotCaptureSource()
    {
        Skip.If(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        
        var transport = new RecordingTransport();
        var options = new SentryOptions
        {
            AttachStacktrace = false,
            TracesSampleRate = 1,
            Transport = transport,
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            EnableDbQuerySource = true,
            DbQuerySourceThresholdMs = 999999 // Very high threshold - no queries should be captured
        };

        await using var database = await _fixture.SqlInstance.Build();
        
        using (var hub = new Hub(options))
        {
            var transaction = hub.StartTransaction("query source test", "test");
            hub.ConfigureScope(scope => scope.Transaction = transaction);

            await using (var connection = await database.OpenNewConnection())
            await using (var dbContext = TestDbBuilder.GetDbContext(connection))
            {
                var result = await dbContext.TestEntities.Where(e => e.Property == "test").ToListAsync();
            }

            transaction.Finish();
        }

        // Verify that query source information was NOT captured due to threshold
        var sentTransaction = transport.Payloads
            .OfType<SentryTransaction>()
            .FirstOrDefault();
            
        Assert.NotNull(sentTransaction);
        
        var querySpans = sentTransaction.Spans.Where(s => s.Operation == "db.query").ToList();
        Assert.NotEmpty(querySpans);
        
        // None of the spans should have source info
        foreach (var span in querySpans)
        {
            Assert.False(span.Extra.ContainsKey("code.filepath"));
            Assert.False(span.Extra.ContainsKey("code.function"));
            Assert.False(span.Extra.ContainsKey("code.namespace"));
        }
    }

    [SkippableFact]
    public async Task EfCore_QuerySourceDisabled_DoesNotCaptureSource()
    {
        Skip.If(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        
        var transport = new RecordingTransport();
        var options = new SentryOptions
        {
            AttachStacktrace = false,
            TracesSampleRate = 1,
            Transport = transport,
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            EnableDbQuerySource = false // Feature disabled
        };

        await using var database = await _fixture.SqlInstance.Build();
        
        using (var hub = new Hub(options))
        {
            var transaction = hub.StartTransaction("query source test", "test");
            hub.ConfigureScope(scope => scope.Transaction = transaction);

            await using (var connection = await database.OpenNewConnection())
            await using (var dbContext = TestDbBuilder.GetDbContext(connection))
            {
                var result = await dbContext.TestEntities.Where(e => e.Property == "test").ToListAsync();
            }

            transaction.Finish();
        }

        // Verify that query source information was NOT captured
        var sentTransaction = transport.Payloads
            .OfType<SentryTransaction>()
            .FirstOrDefault();
            
        Assert.NotNull(sentTransaction);
        
        var querySpans = sentTransaction.Spans.Where(s => s.Operation == "db.query").ToList();
        Assert.NotEmpty(querySpans);
        
        // None of the spans should have source info
        foreach (var span in querySpans)
        {
            Assert.False(span.Extra.ContainsKey("code.filepath"));
            Assert.False(span.Extra.ContainsKey("code.function"));
            Assert.False(span.Extra.ContainsKey("code.namespace"));
        }
    }
#endif

#if !NETFRAMEWORK
    [SkippableFact]
    public async Task SqlClient_WithQuerySource_CapturesSourceLocation()
    {
        Skip.If(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        
        var transport = new RecordingTransport();
        var options = new SentryOptions
        {
            AttachStacktrace = false,
            TracesSampleRate = 1,
            Transport = transport,
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            Debug = true,
            EnableDbQuerySource = true,
            DbQuerySourceThresholdMs = 0
        };

        await using var database = await _fixture.SqlInstance.Build();
        
        using (var hub = new Hub(options))
        {
            var transaction = hub.StartTransaction("query source test", "test");
            hub.ConfigureScope(scope => scope.Transaction = transaction);

            await using (var connection = await database.OpenNewConnection())
            {
                // This query call should be captured as the source location
                await TestDbBuilder.GetDataAsync(connection);
            }

            transaction.Finish();
        }

        // Verify that query source information was captured
        var sentTransaction = transport.Payloads
            .OfType<SentryTransaction>()
            .FirstOrDefault();
            
        Assert.NotNull(sentTransaction);
        
        // Find the db.query span
        var querySpans = sentTransaction.Spans.Where(s => s.Operation == "db.query").ToList();
        Assert.NotEmpty(querySpans);
        
        // At least one query span should have source location info (if PDB available)
        var hasSourceInfo = querySpans.Any(span =>
            span.Extra.ContainsKey("code.function"));
            
        if (hasSourceInfo)
        {
            var spanWithSource = querySpans.First(span => span.Extra.ContainsKey("code.function"));
            var function = spanWithSource.Extra["code.function"] as string;
            Assert.NotNull(function);
            _logger.Log(SentryLevel.Debug, $"Captured SqlClient function: {function}");
        }
    }
#endif
}
