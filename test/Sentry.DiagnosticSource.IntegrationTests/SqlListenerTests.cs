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
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLevel = SentryLevel.Debug
        };

        options.AddIntegration(new SentryDiagnosticListenerIntegration());

        var database = await _fixture.SqlInstance.Build();
#if NET5_0 //TODO: Change to NET5_0_OR_GREATER after updating for https://github.com/SimonCropp/LocalDb/pull/422
        await using (database)
#else
        using (database)
#endif
        using (var hub = new Hub(options))
        {
            var transaction = hub.StartTransaction("my transaction", "my operation");
            hub.ConfigureScope(scope => scope.Transaction = transaction);
            hub.CaptureException(new Exception("my exception"));
            await TestDbBuilder.AddData(database);
            await TestDbBuilder.GetData(database);
            transaction.Finish();
        }

        var payloads = transport.Envelopes
            .SelectMany(x => x.Items)
            .Select(x => x.Payload)
            .ToList();

        await Verify(payloads)
            .IgnoreStandardSentryMembers();
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
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLevel = SentryLevel.Debug
        };

        options.AddIntegration(new SentryDiagnosticListenerIntegration());

        var database = await _fixture.SqlInstance.Build();
#if NET5_0 //TODO: Change to NET5_0_OR_GREATER after updating for https://github.com/SimonCropp/LocalDb/pull/422
        await using (database)
#else
        using (database)
#endif
        using (var hub = new Hub(options))
        {
            var transaction = hub.StartTransaction("my transaction", "my operation");
            hub.ConfigureScope(scope => scope.Transaction = transaction);
            hub.CaptureException(new Exception("my exception"));
            await TestDbBuilder.AddEfData(database);
            await TestDbBuilder.GetEfData(database);
            transaction.Finish();
        }

        var payloads = transport.Envelopes
            .SelectMany(x => x.Items)
            .Select(x => x.Payload)
            .ToList();
        await Verify(payloads)
            .IgnoreStandardSentryMembers()
            .UniqueForRuntimeAndVersion();
    }
}
