#if NET6_0_OR_GREATER
using System.Collections.Concurrent;
using System.Data.Common;
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
        var transport = new RecordingTransport();
        var options = new SentryOptions
        {
            TracesSampleRate = .5,
            Transport = transport,
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            DiagnosticLevel = SentryLevel.Debug
        };

        using var database = await sqlInstance.Build();
        options.AddIntegration(new SentryDiagnosticListenerIntegration());
        using (var sdk = SentrySdk.Init(options))
        {
            var transaction = SentrySdk.StartTransaction("sdf", "sdf");
            SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);
            SentrySdk.CaptureException(new Exception("my other error"));
            await TestDbBuilder.AddData(database);
            await TestDbBuilder.GetData(database);
            sdk.Dispose();
            transaction.Finish();
        }

        await Verify(transport)
            .ModifySerialization(
                _ =>
                {
                    _.IgnoreMember<SentryEvent>(_ => _.Modules);
                    _.IgnoreMembers<Span>(
                        _ => _.SpanId,
                        _ => _.ParentSpanId,
                        _ => _.StartTimestamp,
                        _ => _.EndTimestamp,
                        _ => _.TraceId
                    );
                    _.IgnoreMembers<Trace>(
                        _ => _.SpanId,
                        _ => _.ParentSpanId,
                        _ => _.TraceId
                    );
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

class RecordingTransport : ITransport
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
