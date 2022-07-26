using System.Data.Entity;
using System.Runtime.InteropServices;

namespace Sentry.EntityFramework.Tests;

[UsesVerify]
public class IntegrationTests
{
    [SkippableFact]
    public async Task Simple()
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

        options.AddEntityFramework();

        var sqlInstance = new SqlInstance<TestDbContext>(
            connection => new(connection, true));

        using (var database =await sqlInstance.Build())
        using (var hub = new Hub(options))
        {
            var transaction = hub.StartTransaction("my transaction", "my operation");
            hub.ConfigureScope(scope => scope.Transaction = transaction);
            hub.CaptureException(new Exception("my exception"));
            await database.AddData(
                new TestDbContext.TestData
                {
                    Id = 1,
                    AColumn = "SHOULD NOT APPEAR IN PAYLOAD",
                    RequiredColumn = "Value"
                });
            await database.Context.TestTable.ToListAsync();
            transaction.Finish();
        }

        var payloads = transport.Envelopes
            .SelectMany(x => x.Items)
            .Select(x => x.Payload)
            .ToList();
        var result = await Verify(payloads)
            .IgnoreStandardSentryMembers()
            .UniqueForRuntimeAndVersion()
            //ignore sql version check
            .IgnoreInstance<Span>(_ => _.Description == "select cast(serverproperty('EngineEdition') as int)");
        Assert.DoesNotContain("SHOULD NOT APPEAR IN PAYLOAD", result.Text);
    }
}
