using System.Data.Common;
using System.Diagnostics;
using LocalDb;
using Sentry.Internals.DiagnosticSource;
using Sentry.Testing;
using VerifyTests.Http;

[UsesVerify]
public class SentryDiagnosticSubscriberTests
{
    private static SqlInstance sqlInstance = new(
            name: "SentryDiagnosticSubscriber",
            buildTemplate: TestDbBuilder.CreateTable);

    [Fact]
    public async Task RecordsSql()
    {
        var httpClient = new MockHttpClient();
        var options = new SentryOptions
        {
            Dsn = DsnSamples.ValidDsnWithoutSecret,
            SentryHttpClientFactory = new DelegateHttpClientFactory(_ => httpClient),
            DiagnosticLevel = SentryLevel.Debug
        };
        options.AddIntegration(new SentryDiagnosticListenerIntegration());
        using (var sdk = SentrySdk.Init(options))
        {
            var transaction = SentrySdk.StartTransaction("sdf", "sdf");
            SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);
            SentrySdk.CaptureException(new Exception("my other error"));
            using var database = await sqlInstance.Build();
            await TestDbBuilder.AddData(database);
            await TestDbBuilder.GetData(database);
            transaction.Finish();
        }

        await Verify(httpClient);
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
