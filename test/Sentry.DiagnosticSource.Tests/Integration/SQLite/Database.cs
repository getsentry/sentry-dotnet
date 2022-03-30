using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Sentry.DiagnosticSource.Tests.Integration.SQLite;

public class Database
{
    public readonly DbContextOptions<ItemsContext> ContextOptions;

    public Database()
    {
        ContextOptions = new DbContextOptionsBuilder<ItemsContext>()
            .UseSqlite(CreateInMemoryDatabase())
            .Options;

        _ = RelationalOptionsExtension.Extract(ContextOptions).Connection;
    }

    public void Seed()
    {
        using var context = new ItemsContext(ContextOptions);

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var one = new Item { Name = "ItemOne" };
        var two = new Item { Name = "ItemTwo" };
        var three = new Item { Name = "ItemThree" };

        context.AddRange(one, two, three);
        context.SaveChanges();
    }

    private static DbConnection CreateInMemoryDatabase()
    {
        var connection = new SqliteConnection("Filename=:memory:");

        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        command.ExecuteNonQuery();
        return connection;
    }
}
