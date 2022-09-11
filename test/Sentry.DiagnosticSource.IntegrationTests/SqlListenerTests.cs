using System.Runtime.InteropServices;
using Sentry.Internals.DiagnosticSource;

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

        var database = await _fixture.SqlInstance.Build();
#if NET5_0_OR_GREATER
        await using (database)
#else
        using (database)
#endif
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

        var database = await _fixture.SqlInstance.Build();
#if NET5_0_OR_GREATER
        await using (database)
#else
        using (database)
#endif
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
