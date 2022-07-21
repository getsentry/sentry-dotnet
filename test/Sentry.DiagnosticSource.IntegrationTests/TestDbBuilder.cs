using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

public static class TestDbBuilder
{
    public static async Task CreateTable(SqlConnection connection)
    {
        using var dbContext = GetDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "create table MyTable (Value int);";
        await command.ExecuteNonQueryAsync();
    }

    private static int intData;

    private static TestDbContext GetDbContext(SqlConnection connection)
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>();
        builder.UseSqlServer(connection);
        builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        var dbContext = new TestDbContext(builder.Options);
        return dbContext;
    }

    public static async Task AddEfData(SqlConnection connection)
    {
        using var dbContext = GetDbContext(connection);
        dbContext.TestEntities.Add(new TestEntity { Property = "Value" });
        await dbContext.SaveChangesAsync();
    }

    public static async Task GetEfData(SqlConnection connection)
    {
        using var dbContext = GetDbContext(connection);
        await dbContext.TestEntities.ToListAsync();
    }

    public static async Task AddData(SqlConnection connection)
    {
        using var command = connection.CreateCommand();
        var addData = intData;
        intData++;
        command.CommandText = $@"
insert into MyTable (Value)
values ({addData});";
        await command.ExecuteNonQueryAsync();
    }

    public static async Task GetData(SqlConnection connection)
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
