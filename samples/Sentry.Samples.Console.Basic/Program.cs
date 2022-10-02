#pragma warning disable CS0168
using var _ = SentrySdk.Init(o =>
{
    o.Dsn = "https://e8d57cabda394366b25b57bba7c204a6@o1027677.ingest.sentry.io/5994502";
    o.Debug = true;
    o.TracesSampleRate = 1.0;
    o.IsGlobalModeEnabled = true;
    o.AutoSessionTracking = true;
    o.Environment = "development";
    o.Release = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");
});

var tran = SentrySdk.StartTransaction("name5", "operation5");
// Forgot about this. Not a great API
SentrySdk.ConfigureScope(s => s.Transaction = tran);

try
{
    throw new Exception("Exception5");
}
catch (Exception e)
{
    // Exception is only captured and tied to the transaction if I explicitly capture it here
    throw; // When I rethrow, the session ends as success (no session update it sent, only the initial healthy one)
}
