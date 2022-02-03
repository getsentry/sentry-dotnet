#if !net461
using System.Collections.Concurrent;
using System.Data.Common;
using System.Runtime.InteropServices;
using LocalDb;
using Sentry.Internals.DiagnosticSource;

[UsesVerify]
public class SentryDiagnosticSubscriberTests
{
    private static SqlInstance sqlInstance = new(
            name: "SentryDiagnosticSubscriber",
            buildTemplate: TestDbBuilder.CreateTable);

    [Fact]
    public async Task RecordsSql()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }
        var transport = new RecordingTransport();
        var options = new SentryOptions
        {
            TracesSampleRate = 1,
            Transport = transport,
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLevel = SentryLevel.Debug
        };

        using var database = await sqlInstance.Build();
        options.AddIntegration(new SentryDiagnosticListenerIntegration());
        using (SentrySdk.Init(options))
        {
            var transaction = SentrySdk.StartTransaction("my transaction", "my operation");
            SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);
            SentrySdk.CaptureException(new Exception("my exception"));
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
                _ =>
                {
                    _.IgnoreMembersWithType<Contexts>();
                    _.IgnoreMembersWithType<SdkVersion>();
                    _.IgnoreMembersWithType<DateTimeOffset>();
                    _.IgnoreMembersWithType<SpanId>();
                    _.IgnoreMembersWithType<SentryId>();
                    _.IgnoreMembers<SentryEvent>(_ => _.Modules, _ => _.Release);
                    _.IgnoreMembers<Transaction>(_ => _.Release);
                    _.IgnoreMembers<SentryException>(_ => _.Module, _ => _.ThreadId);
                });
    }

    public static class TestDbBuilder
    {
        public static async Task CreateTable(DbConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "create table MyTable (Value int);";
            await command.ExecuteNonQueryAsync();
        }

        private static int intData;

        public static async Task<int> AddData(DbConnection connection)
        {
            using var command = connection.CreateCommand();
            var addData = intData;
            intData++;
            command.CommandText = $@"
insert into MyTable (Value)
values ({addData});";
            await command.ExecuteNonQueryAsync();
            return addData;
        }

        public static async Task<List<int>> GetData(DbConnection connection)
        {
            var values = new List<int>();
            using var command = connection.CreateCommand();
            command.CommandText = "select Value from MyTable";
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                values.Add(reader.GetInt32(0));
            }

            return values;
        }
    }
}

internal class RecordingTransport : ITransport
{
    private ConcurrentBag<Envelope> envelopes = new();

    public IEnumerable<Envelope> Envelopes => envelopes;

    public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        envelopes.Add(envelope);
        return Task.CompletedTask;
    }
}

#endif
