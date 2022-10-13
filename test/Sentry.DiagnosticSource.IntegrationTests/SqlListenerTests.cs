namespace Sentry.DiagnosticSource.IntegrationTests;

[UsesVerify]
public class SqlListenerTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public SqlListenerTests(LocalDbFixture fixture)
    {
        _fixture = fixture;
    }

#if !NETFRAMEWORK
    [SkippableFact]
    public async Task RecordsSql()
    {
        Skip.If(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        var transport = new RecordingTransport();
        var options = new SentryOptions
        {
            TracesSampleRate = 1,
            Transport = transport,
            Dsn = ValidDsn,
            DiagnosticLevel = SentryLevel.Debug
        };

        options.AddIntegration(new SentryDiagnosticListenerIntegration());

        using (var database = await _fixture.SqlInstance.Build())
        using (var hub = new Hub(options))
        {
            var transaction = hub.StartTransaction("my transaction", "my operation");
            hub.ConfigureScope(scope => scope.Transaction = transaction);
            hub.CaptureException(new("my exception"));
            await TestDbBuilder.AddData(database);
            await TestDbBuilder.GetData(database);
            transaction.Finish();
        }

        var result = await Verify(transport.Payloads)
            .IgnoreStandardSentryMembers();
        Assert.DoesNotContain("SHOULD NOT APPEAR IN PAYLOAD", result.Text);
    }

#endif

#if NET6_0_OR_GREATER
    [SkippableFact]
    public async Task Logging()
    {
        Skip.If(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        var transport = new RecordingTransport();

        void ApplyOptions(SentryOptions sentryOptions)
        {
            sentryOptions.TracesSampleRate = 1;
            sentryOptions.Transport = transport;
            sentryOptions.Dsn = ValidDsn;
            sentryOptions.DiagnosticLevel = SentryLevel.Debug;
        }

        var options = new SentryOptions();
        ApplyOptions(options);

        options.AddIntegration(new SentryDiagnosticListenerIntegration());

        var loggerFactory = LoggerFactory.Create(_ => _.AddSentry(ApplyOptions));

        await using var database = await _fixture.SqlInstance.Build();
        var builder = new DbContextOptionsBuilder<TestDbContext>();
        builder.UseSqlServer(database);
        builder.UseLoggerFactory(loggerFactory);
        builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        await using var dbContext = new TestDbContext(builder.Options);
        dbContext.Add(
            new TestEntity
            {
                Property = "Value"
            });
        await dbContext.SaveChangesAsync();
        using (var hub = new Hub(options))
        {
            var transaction = hub.StartTransaction("my transaction", "my operation");
            hub.ConfigureScope(scope => scope.Transaction = transaction);

            dbContext.Add(
                new TestEntity
                {
                    Property = "Value"
                });
            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch
            {
            }

            transaction.Finish();
        }

        var result = await Verify(transport.Payloads)
            .ScrubInlineGuids()
            .IgnoreMember<SentryEvent>(_ => _.SentryThreads)
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

                var efVersion = typeof(DbContext).Assembly.GetName().Version.ToString(3);
                return line.Replace(efVersion, "");
            })
            .IgnoreStandardSentryMembers();
        Assert.DoesNotContain("An error occurred while saving the entity changes", result.Text);
    }

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

#endif

    [SkippableFact]
    public async Task RecordsEf()
    {
        Skip.If(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        var transport = new RecordingTransport();
        var options = new SentryOptions
        {
            TracesSampleRate = 1,
            Transport = transport,
            Dsn = ValidDsn,
            DiagnosticLevel = SentryLevel.Debug
        };

        options.AddIntegration(new SentryDiagnosticListenerIntegration());

        using (var database = await _fixture.SqlInstance.Build())
        using (var hub = new Hub(options))
        {
            var transaction = hub.StartTransaction("my transaction", "my operation");
            hub.ConfigureScope(scope => scope.Transaction = transaction);
            hub.CaptureException(new("my exception"));
            await TestDbBuilder.AddEfData(database);
            await TestDbBuilder.GetEfData(database);
            transaction.Finish();
        }

        var result = await Verify(transport.Payloads)
            .IgnoreStandardSentryMembers()
            .UniqueForRuntimeAndVersion();
        Assert.DoesNotContain("SHOULD NOT APPEAR IN PAYLOAD", result.Text);
    }
}
