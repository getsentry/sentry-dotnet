namespace Sentry.DiagnosticSource.IntegrationTests;

public static class TestDbBuilder
{
    public static async Task CreateTable(SqlConnection connection)
    {
        using var dbContext = GetDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "create table MyTable (Value nvarchar(100));";
        await command.ExecuteNonQueryAsync();
    }

    private static TestDbContext GetDbContext(SqlConnection connection)
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>();
        builder.UseSqlServer(connection);
        builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        return new(builder.Options);
    }

    public static async Task AddEfData(SqlConnection connection)
    {
        using var dbContext = GetDbContext(connection);
        dbContext.Add(
            new TestEntity
            {
                Property = "SHOULD NOT APPEAR IN PAYLOAD"
            });
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
        command.Parameters.AddWithValue("value", "SHOULD NOT APPEAR IN PAYLOAD");
        command.CommandText = @"
insert into MyTable (Value)
values (@value);";
        await command.ExecuteNonQueryAsync();
    }

    public static async Task GetData(SqlConnection connection)
    {
        var values = new List<string>();
        using var command = connection.CreateCommand();
        command.Parameters.AddWithValue("value", "SHOULD NOT APPEAR IN PAYLOAD");
        command.CommandText = "select Value from MyTable where Value = @value";
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }
    }
}
