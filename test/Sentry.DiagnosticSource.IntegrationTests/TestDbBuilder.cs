namespace Sentry.DiagnosticSource.IntegrationTests;

public static class TestDbBuilder
{
    public static async Task CreateTableAsync(SqlConnection connection)
    {
        await using var dbContext = GetDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();

#if NETFRAMEWORK
        using var command = connection.CreateCommand();
#else
        await using var command = connection.CreateCommand();
#endif

        command.CommandText = "create table MyTable (Value nvarchar(100));";
        await command.ExecuteNonQueryAsync();
    }

    public static TestDbContext GetDbContext(SqlConnection connection, ILoggerFactory loggerFactory = null)
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>();
        builder.UseSqlServer(connection);
        builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        if (loggerFactory != null)
        {
            builder.UseLoggerFactory(loggerFactory);
        }

        return new TestDbContext(builder.Options);
    }

    public static async Task AddEfDataAsync(SqlConnection connection)
    {
        await using var dbContext = GetDbContext(connection);
        dbContext.Add(
            new TestEntity
            {
                Property = "SHOULD NOT APPEAR IN PAYLOAD"
            });
        await dbContext.SaveChangesAsync();
    }

    public static async Task GetEfDataAsync(SqlConnection connection)
    {
        await using var dbContext = GetDbContext(connection);
        await dbContext.TestEntities.ToListAsync();
    }

    public static async Task AddDataAsync(SqlConnection connection)
    {
#if NETFRAMEWORK
        using var command = connection.CreateCommand();
#else
        await using var command = connection.CreateCommand();
#endif

        command.Parameters.AddWithValue("value", "SHOULD NOT APPEAR IN PAYLOAD");
        command.CommandText = @"
insert into MyTable (Value)
values (@value);";
        await command.ExecuteNonQueryAsync();
    }

    public static async Task<List<string>> GetDataAsync(SqlConnection connection)
    {
#if NETFRAMEWORK
        using var command = connection.CreateCommand();
#else
        await using var command = connection.CreateCommand();
#endif

        command.Parameters.AddWithValue("value", "SHOULD NOT APPEAR IN PAYLOAD");
        command.CommandText = "select Value from MyTable where Value = @value";

#if NETFRAMEWORK
        using var reader = await command.ExecuteReaderAsync();
#else
        await using var reader = await command.ExecuteReaderAsync();
#endif

        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }
}
