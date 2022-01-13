using System.Data.Common;
using System.Diagnostics;
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
        var hub = Substitute.For<IHub>();
        using (var subscriber = new SentryDiagnosticSubscriber(hub, new SentryOptions()))
        using (DiagnosticListener.AllListeners.Subscribe(subscriber))
        {
            using var database = await sqlInstance.Build();
            await TestDbBuilder.AddData(database);
            await TestDbBuilder.GetData(database);
        }

        List<ICall> receivedCalls = hub.ReceivedCalls().ToList();
        var enumerable = receivedCalls.Select(x=>x.GetArguments().Single()).Cast<Action<Scope>>().ToList();
        await Verifier.Verify(receivedCalls);
    }

    public static class TestDbBuilder
    {
        public static async Task CreateTable(DbConnection connection)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "create table MyTable (Value int);";
            await command.ExecuteNonQueryAsync();
        }

        static int intData = 0;

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
