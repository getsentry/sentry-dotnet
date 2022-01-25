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
        var options = new SentryOptions();
        options.Dsn = DsnSamples.ValidDsnWithoutSecret;
        var httpClient = new MockHttpClient();
        options.SentryHttpClientFactory = new DelegateHttpClientFactory(_ => httpClient);
        var hub = SentrySdk.InitHub(options);
        using (var subscriber = new SentryDiagnosticSubscriber(hub, options))
        using (DiagnosticListener.AllListeners.Subscribe(subscriber))
        {
            using var database = await sqlInstance.Build();
            await TestDbBuilder.AddData(database);
            await TestDbBuilder.GetData(database);
        }

        await Verify(httpClient);
    }

    public static class TestDbBuilder
    {
        public static async Task CreateTable(DbConnection connection)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "create table MyTable (Value int);";
            await command.ExecuteNonQueryAsync();
        }

        private static int intData;

        public static async Task<int> AddData(DbConnection connection)
        {
            await using var command = connection.CreateCommand();
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
            await using var command = connection.CreateCommand();
            command.CommandText = "select Value from MyTable";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                values.Add(reader.GetInt32(0));
            }

            return values;
        }
    }
}
