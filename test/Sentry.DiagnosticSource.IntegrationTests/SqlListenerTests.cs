using System.Runtime.InteropServices;
using LocalDb;
using Sentry.Internals.DiagnosticSource;

[UsesVerify]
public class SqlListenerTests
{
    private static SqlInstance sqlInstance;

    static SqlListenerTests()
    {
        sqlInstance = new SqlInstance(
            name: "SqlListenerTests" + Namer.RuntimeAndVersion,
            buildTemplate: TestDbBuilder.CreateTable);
    }

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

        using var database = await sqlInstance.Build();
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
            .ModifySerialization(
                p =>
                {
                    p.IgnoreMembersWithType<Contexts>();
                    p.IgnoreMembersWithType<SdkVersion>();
                    p.IgnoreMembersWithType<DateTimeOffset>();
                    p.IgnoreMembersWithType<SpanId>();
                    p.IgnoreMembersWithType<SentryId>();
                    p.IgnoreMembers<SentryEvent>(e => e.Modules, e => e.Release);
                    p.IgnoreMembers<Transaction>(t => t.Release);
                    p.IgnoreMembers<SentryException>(e => e.Module, e => e.ThreadId);
                });
    }

}
