using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Sentry;

var _ = SentrySdk.Init(o =>
{
    o.Debug = true; // To see SDK logs on the console
    o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
    o.TracesSampleRate = 1;
    // Add the EntityFramework integration to the SentryOptions of your app startup code:
    o.AddEntityFramework();
});

var dbConnection = Effort.DbConnectionFactory.CreateTransient();
dbConnection.SetConnectionTimeout(60);
using var db = new SampleDbContext(dbConnection, true);

var transaction = SentrySdk.StartTransaction("Some Http Post request", "Create");

//Populate the database
for (int j=0; j < 10; j++)
{
    _ = db.Users.Add(new SampleUser() { Id = j, RequiredColumn = "123" });
}
db.Users.Add(new SampleUser() { Id = 52, RequiredColumn = "Bill" });


// This will throw a DbEntityValidationException and crash the app
// But Sentry will capture the error.
db.SaveChanges();

transaction.Finish();
transaction = SentrySdk.StartTransaction("Some Http Search", "Create");
var query = db.Users
        .Where(s => s.RequiredColumn == "Bill")
        .FirstOrDefault<SampleUser>();
transaction.Finish();

public class SampleUser : IDisposable
{
    private int _id { get; set; }

    [Key]
    public int Id
    {
        get
        {
            Task.Delay(150).Wait();
            return _id;
        }
        set
        {
            _id = value;
        }
    }
    [Required]
    public string RequiredColumn { get; set; }

    public void Dispose() { }
}

public class SampleDbContext : DbContext
{
    public DbSet<SampleUser> Users { get; set; }
    public SampleDbContext(DbConnection connection, bool ownsConnection)
        : base(connection, ownsConnection)
    { }
}
