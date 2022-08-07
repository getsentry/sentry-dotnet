namespace Sentry.EntityFramework.Tests;

public class TestDbContext : DbContext
{
    public TestDbContext(DbConnection connection, bool ownsConnection)
        : base(connection, ownsConnection)
    { }

    public virtual DbSet<TestData> TestTable { get; set; }

    public class TestData
    {
        [Key]
        public int Id { get; set; }
        public string AColumn { get; set; }
        [Required]
        public string RequiredColumn { get; set; }
    }
}
