namespace Sentry.DiagnosticSource.IntegrationTests.EF;

public class TestDbContext :
    DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    public TestDbContext(DbContextOptions options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model)
        => model.Entity<TestEntity>()
            .HasIndex(u => u.Property)
            .IsUnique();
}
