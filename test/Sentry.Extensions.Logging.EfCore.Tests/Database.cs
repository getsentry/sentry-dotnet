using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Sentry.Extensions.Logging.EfCore.Tests
{
    public class Database
    {
        public readonly DbContextOptions<ItemsContext> ContextOptions;
        private readonly DbConnection _connection;


        public Database()
        {
            ContextOptions = new DbContextOptionsBuilder<ItemsContext>()
            .UseSqlite(CreateInMemoryDatabase())
            .Options;

            _connection = RelationalOptionsExtension.Extract(ContextOptions).Connection;

            Seed();
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

        DbConnection CreateInMemoryDatabase()
        {
            var connection = new SqliteConnection("Filename=:memory:");

            connection.Open();

            return connection;
        }
    }
}
