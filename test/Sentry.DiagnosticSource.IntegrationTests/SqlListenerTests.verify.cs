namespace Sentry.DiagnosticSource.IntegrationTests;

public class SqlListenerTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;
    private readonly TestOutputDiagnosticLogger _logger;

    public SqlListenerTests(LocalDbFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _logger = new TestOutputDiagnosticLogger(output);
    }

#if !NETFRAMEWORK
    [SkippableFact]
    public async Task RecordsSqlAsync()
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
            Debug = true
        };

#if NET6_0_OR_GREATER
        await using var database = await _fixture.SqlInstance.Build();
#else
        using var database = await _fixture.SqlInstance.Build();
#endif
        using (var hub = new Hub(options))
        {
            var transaction = hub.StartTransaction("my transaction", "my operation");
            hub.ConfigureScope(scope => scope.Transaction = transaction);
            hub.CaptureException(new("my exception"));

            await using (var connection = await database.OpenNewConnection())
            {
                await TestDbBuilder.AddDataAsync(connection);
                await TestDbBuilder.GetDataAsync(connection);
            }

            transaction.Finish();
        }

        var result = await Verify(transport.Payloads)
            .IgnoreMember<IEventLike>(_ => _.Environment)
            .ScrubLinesWithReplace(line => line.Replace(LocalDbFixture.InstanceName, "SqlListenerTests"))
            .IgnoreStandardSentryMembers();
        Assert.DoesNotContain("SHOULD NOT APPEAR IN PAYLOAD", result.Text);
    }
#endif

#if NET6_0_OR_GREATER
    [SkippableFact]
    public async Task LoggingAsync()
    {
        Skip.If(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        var transport = new RecordingTransport();

        void ApplyOptions(SentryLoggingOptions sentryOptions)
        {
            sentryOptions.AttachStacktrace = false;
            sentryOptions.TracesSampleRate = 1;
            sentryOptions.Transport = transport;
            sentryOptions.Dsn = ValidDsn;
            sentryOptions.DiagnosticLogger = _logger;
            sentryOptions.Debug = true;
        }

        var options = new SentryLoggingOptions();
        ApplyOptions(options);

        await using var database = await _fixture.SqlInstance.Build();

        await using (var dbContext = TestDbBuilder.GetDbContext(database.Connection))
        {
            dbContext.Add(new TestEntity
            {
                Property = "Value"
            });

            await dbContext.SaveChangesAsync();
        }

        var loggerFactory = LoggerFactory.Create(_ => _.AddSentry(ApplyOptions));
        using (var hub = new Hub(options))
        {
            var transaction = hub.StartTransaction("my transaction", "my operation");
            hub.ConfigureScope(scope => scope.Transaction = transaction);

            await using (var connection = await database.OpenNewConnection())
            await using (var dbContext = TestDbBuilder.GetDbContext(connection, loggerFactory))
            {
                dbContext.Add(new TestEntity
                {
                    Property = "Value"
                });

                try
                {
                    await dbContext.SaveChangesAsync();
                }
                catch
                {
                    // Suppress the exception so we can test that we received the error through logging.
                    // Note, this uses the Sentry.Extensions.Logging integration.
                }
            }

            transaction.Finish();
        }

        var result = await Verify(transport.Payloads)
            .ScrubInlineGuids()
            .IgnoreMember<IEventLike>(_ => _.Environment)
            .ScrubLinesWithReplace(line => line.Replace(LocalDbFixture.InstanceName, "SqlListenerTests"))

            // Really not sure why, but bytes received for this test varies randomly when run in CI
            // TODO: remove this and investigate
            .IgnoreMember("bytes_received")

            .ScrubLinesWithReplace(line =>
            {
                if (line.StartsWith("Executed DbCommand ("))
                {
                    return "Executed DbCommand";
                }

                if (line.StartsWith("Failed executing DbCommand ("))
                {
                    return "Failed executing DbCommand";
                }

                var efVersion = typeof(DbContext).Assembly.GetName().Version!.ToString(3);
                return line.Replace(efVersion, "");
            })
            .IgnoreStandardSentryMembers()
            .UniqueForRuntimeAndVersion();
        Assert.DoesNotContain("An error occurred while saving the entity changes", result.Text);
    }
#endif

    [Fact]
    public void ShouldIgnoreAllErrorAndExceptionIds()
    {
        var eventIds = typeof(CoreEventId).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.FieldType == typeof(EventId))
            .ToList();
        Assert.NotEmpty(eventIds);
        foreach (var field in eventIds)
        {
            var eventId = (EventId)field.GetValue(null)!;
            var isEfExceptionMessage = SentryLogger.IsEfExceptionMessage(eventId);
            var name = field.Name;
            if (name.EndsWith("Exception") ||
                name.EndsWith("Error") ||
                name.EndsWith("Failed"))
            {
                Assert.True(isEfExceptionMessage, eventId.Name);
            }
            else
            {
                Assert.False(isEfExceptionMessage, eventId.Name);
            }
        }
    }

    [SkippableFact]
    public async Task RecordsEfAsync()
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
            Debug = true
        };

#if NETFRAMEWORK
        options.AddDiagnosticSourceIntegration();
#endif

#if NET6_0_OR_GREATER
        await using var database = await _fixture.SqlInstance.Build();
#else
        using var database = await _fixture.SqlInstance.Build();
#endif
        using (var hub = new Hub(options))
        {
            var transaction = hub.StartTransaction("my transaction", "my operation");
            hub.ConfigureScope(scope => scope.Transaction = transaction);
            hub.CaptureException(new("my exception"));

#if NETCOREAPP
            await using (var connection = await database.OpenNewConnection())
#else
            using (var connection = await database.OpenNewConnection())
#endif
            {
                await TestDbBuilder.AddEfDataAsync(connection);
                await TestDbBuilder.GetEfDataAsync(connection);
            }

            transaction.Finish();
        }

        var result = await Verify(transport.Payloads)
            .IgnoreMember<IEventLike>(_ => _.Environment)
            .ScrubLinesWithReplace(line => line.Replace(LocalDbFixture.InstanceName, "SqlListenerTests"))
            .IgnoreStandardSentryMembers()
            .UniqueForRuntimeAndVersion();
        Assert.DoesNotContain("SHOULD NOT APPEAR IN PAYLOAD", result.Text);
    }
}
