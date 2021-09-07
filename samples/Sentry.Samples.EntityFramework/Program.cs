using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using Sentry;

_ = SentrySdk.Init(o =>
{
    o.Debug = true; // To see SDK logs on the console
    o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
    o.TracesSampleRate = 1;
    // Add the EntityFramework integration to the SentryOptions of your app startup code:
    o.AddEntityFramework();
});

var dbConnection = Effort.DbConnectionFactory.CreateTransient();
dbConnection.SetConnectionTimeout(60);
var db = new SampleDbContext(dbConnection, true);

// ========================= Insert Requests ==================
//
// ============================================================
Console.WriteLine("Some Http Post request");
SentrySdk.ConfigureScope(scope =>
{
    scope.Transaction = SentrySdk.StartTransaction("/Start", "Create");


    var manualSpan = scope.Transaction.StartChild("Database Fill");

    //Populate the database
    for (var j = 0; j < 10; j++)
    {
        _ = db.Users.Add(new SampleUser { Id = j, RequiredColumn = "123" });
    }
    db.Users.Add(new SampleUser { Id = 52, RequiredColumn = "Bill" });
    manualSpan.Finish();

    // This will throw a DbEntityValidationException and crash the app
    // But Sentry will capture the error.
    manualSpan = scope.Transaction.StartChild("Save changes");
    db.SaveChanges();
    manualSpan.Finish();
    scope.Transaction.Finish();
});

// ========================= Search Request ===================
//
// ============================================================
SentrySdk.ConfigureScope(scope =>
{
    scope.Transaction = SentrySdk.StartTransaction("/Users?name=Bill", "GET");
    Console.WriteLine("Searching for users named Bill");

    var manualSpan = scope.Transaction.StartChild("manual - search");
    var query = db.Users
        .Where(s => s.RequiredColumn == "Bill")
        .ToList();
    manualSpan.Finish();
    scope.Transaction.Finish();
    Console.WriteLine($"Found {query.Count} users.");
});

SentrySdk.ConfigureScope(scope =>
{
    Console.WriteLine("Searching for users with Id higher than 5...");
    scope.Transaction = SentrySdk.StartTransaction("/Users?id>5", "GET");
    var query2 = db.Users.Where(user => user.Id > 5).ToList();
    scope.Transaction.Finish();
    Console.WriteLine($"Found {query2.Count} users.");
});


public class SampleUser : IDisposable
{
    private int _id { get; set; }

    [Key]
    public int Id
    {
        get => _id;
        set => _id = value;
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
