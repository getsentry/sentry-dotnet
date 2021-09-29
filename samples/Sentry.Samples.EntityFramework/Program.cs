using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.Entity;
using Sentry;

var _ = SentrySdk.Init(o =>
{
    o.Debug = true; // To see SDK logs on the console
    o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
    // Add the EntityFramework integration to the SentryOptions of your app startup code:
    o.AddEntityFramework();
});

var dbConnection = Effort.DbConnectionFactory.CreateTransient();
using var db = new SampleDbContext(dbConnection, true);

var user = new SampleUser();
db.Users.Add(user);

// This will throw a DbEntityValidationException and crash the app
// But Sentry will capture the error.
db.SaveChanges();

public class SampleUser
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string RequiredColumn { get; set; }
}

public class SampleDbContext : DbContext
{
    public DbSet<SampleUser> Users { get; set; }
    public SampleDbContext(DbConnection connection, bool ownsConnection)
        : base(connection, ownsConnection)
    { }
}
