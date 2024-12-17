#if NETFRAMEWORK
namespace Sentry.EntityFramework.Tests;

[Collection("Sequential")]
public class IntegrationTests
{
    // needs to be a variable to stop EF from inlining it as a constant
    static string shouldNotAppearInPayload = "SHOULD NOT APPEAR IN PAYLOAD";

    [SkippableFact]
    public async Task Simple()
    {
        Skip.If(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        var transport = new RecordingTransport();
        var options = new SentryOptions
        {
            AttachStacktrace = false,
            TracesSampleRate = 1,
            Transport = transport,
            Dsn = ValidDsn,
            DiagnosticLevel = SentryLevel.Debug,
            Release = "test-release"
        };

        options.AddEntityFramework();

        var sqlInstance = new SqlInstance<TestDbContext>(
            constructInstance: connection => new(connection, true));

        using (var database = await sqlInstance.Build())
        {
            // forcing a query means EF performs it startup version checks against sql.
            // so we dont include that noise in assert
            await database.Context.TestTable.ToListAsync();

            using (var hub = new Hub(options))
            {
                var transaction = hub.StartTransaction("my transaction", "my operation");
                hub.ConfigureScope(scope => scope.Transaction = transaction);
                hub.CaptureException(new("my exception"));
                await database.AddData(
                    new TestDbContext.TestData
                    {
                        Id = 1,
                        AColumn = shouldNotAppearInPayload,
                        RequiredColumn = "Value"
                    });
                await database.Context.TestTable
                    .FirstAsync(_ => _.AColumn == shouldNotAppearInPayload);
                transaction.Finish();
            }
        }

        var result = await Verify(transport.Payloads)
            .IgnoreStandardSentryMembers();
        Assert.DoesNotContain(shouldNotAppearInPayload, result.Text);
        options.DisableDbInterceptionIntegration();
    }
}

#endif
