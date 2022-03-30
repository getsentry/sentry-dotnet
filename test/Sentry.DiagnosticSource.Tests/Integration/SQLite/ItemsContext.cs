using Microsoft.EntityFrameworkCore;

namespace Sentry.DiagnosticSource.Tests.Integration.SQLite;

public class ItemsContext : DbContext
{
    public ItemsContext(DbContextOptions options) : base(options) { }

    public DbSet<Item> Items { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(
            b =>
            {
                b.Property("Id");
                b.HasKey("Id");
                b.Property(e => e.Name);
            });
    }
}
