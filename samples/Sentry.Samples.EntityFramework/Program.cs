using System;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
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

// ========================= Insert Requests ==================
//
// ============================================================
var transaction = SentrySdk.StartTransaction("Some Http Post request", "Create");
Console.WriteLine("Some Http Post request");
SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);
ISpan manualSpan;


//Populate the database
for (int j = 0; j < 10; j++)
{
    manualSpan = SentrySdk.GetSpan().StartChild("manual - create item");
    _ = db.Users.Add(new SampleUser() { Id = j, RequiredColumn = "123" });
    manualSpan.Finish();
}
manualSpan = SentrySdk.GetSpan().StartChild("manual - create item");
db.Users.Add(new SampleUser() { Id = 52, RequiredColumn = "Bill" });
manualSpan.Finish();


// This will throw a DbEntityValidationException and crash the app
// But Sentry will capture the error.
manualSpan = SentrySdk.GetSpan().StartChild("manual - save changes");
db.SaveChanges();
manualSpan.Finish();
transaction.Finish();

// ========================= Search Request ===================
//
// ============================================================
transaction = SentrySdk.StartTransaction("Some Http Search", "Create");
Console.WriteLine("Some Http Search");
SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

manualSpan = SentrySdk.GetSpan().StartChild("manual - search");
var query = db.Users
        .Where(s => s.RequiredColumn == "Bill")
        .ToList();
manualSpan.Finish();
transaction.Finish();
Console.WriteLine($"Found {query.Count}");

Console.WriteLine("Text SQL Search");
transaction = SentrySdk.StartTransaction("Text SQL Search", "Create");
SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);
var query2 = db.Users.Where(user => user.Id > 5).ToList();
transaction.Finish();
Console.WriteLine($"Found {query2.Count}");


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
