using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.Entity;

using var _ = SentrySdk.Init(options =>
{
    // You can set here in code, or you can set it in the SENTRY_DSN environment variable.
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";

    options.Debug = true; // To see SDK logs on the console
    options.TracesSampleRate = 1.0;

    // Add the EntityFramework integration to the SentryOptions of your app startup code:
    options.AddEntityFramework();
});

var dbConnection = Effort.DbConnectionFactory.CreateTransient();
dbConnection.SetConnectionTimeout(60);
var db = new SampleDbContext(dbConnection, true);

// This creates a transaction where each insertion will be registered into the active transaction.
Console.WriteLine("Some Http Post request");
SentrySdk.ConfigureScope(scope =>
{
    scope.Transaction = SentrySdk.StartTransaction("/Start", "Create");
    var manualSpan = scope.Transaction.StartChild("Database Fill");

    //Populate the database
    for (var j = 0; j < 10; j++)
    {
        db.Users.Add(new SampleUser { Id = j, RequiredColumn = "123" });
    }
    db.Users.Add(new SampleUser { Id = 52, RequiredColumn = "Bill" });
    manualSpan.Finish();

    manualSpan = scope.Transaction.StartChild("Save changes");
    db.SaveChanges();
    manualSpan.Finish();
    scope.Transaction.Finish();
});

// This simulates a search operation, creating a new transaction for the requested data.
SentrySdk.ConfigureScope(scope =>
{
    Console.WriteLine("Searching for users named Bill");
    scope.Transaction = SentrySdk.StartTransaction("/Users?name=Bill", "GET");
    var manualSpan = scope.Transaction.StartChild("manual - search");

    var query = db.Users
        .Where(s => s.RequiredColumn == "Bill")
        .ToList();

    manualSpan.Finish();
    scope.Transaction.Finish();
    Console.WriteLine($"Found {query.Count} users.");
});

// This simulates a search operation, creating a new transaction for the requested data.
SentrySdk.ConfigureScope(scope =>
{
    Console.WriteLine("Searching for users with Id higher than 5...");
    scope.Transaction = SentrySdk.StartTransaction("/Users?id>5", "GET");

    var query2 = db.Users.Where(user => user.Id > 5).ToList();

    scope.Transaction.Finish();
    Console.WriteLine($"Found {query2.Count} users.");
});

// This will throw a DbEntityValidationException and crash the app
// But Sentry will capture the error.
var user = new SampleUser();
db.Users.Add(user);
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
