using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;

namespace Sentry.EntityFramework.Tests;

public class TestDbContext : DbContext
{
    public TestDbContext(DbConnection connection, bool ownsConnection)
        : base(connection, ownsConnection)
    {
    }

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

public class CustomDbConfiguration :
    DbConfiguration,
    IManifestTokenResolver
{
    static DefaultManifestTokenResolver defaultResolver = new();

    public CustomDbConfiguration()
    {
        SetManifestTokenResolver(this);
    }

    public string ResolveManifestToken(DbConnection connection)
    {
        if (connection is SqlConnection)
        {
            return "2012";
        }

        return defaultResolver.ResolveManifestToken(connection);
    }
}
