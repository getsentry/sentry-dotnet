using System.Data.Common;

public static class TestDbBuilder
{
    public static async Task CreateTable(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "create table MyTable (Value int);";
        await command.ExecuteNonQueryAsync();
    }

    private static int intData;

    public static async Task AddData(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        var addData = intData;
        intData++;
        command.CommandText = $@"
insert into MyTable (Value)
values ({addData});";
        await command.ExecuteNonQueryAsync();
    }

    public static async Task GetData(DbConnection connection)
    {
        var values = new List<int>();
        using var command = connection.CreateCommand();
        command.CommandText = "select Value from MyTable";
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetInt32(0));
        }
    }
}
