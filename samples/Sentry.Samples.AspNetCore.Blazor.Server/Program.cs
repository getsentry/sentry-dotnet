// Capture blazor bootstrapping errors

var configureSentry = new Action<SentryOptions>(
    options =>
{
    options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
    options.Debug = true;
});
using var sdk = SentrySdk.Init(configureSentry);
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.AddSentry(configureSentry);
    // Add services to the container.
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
    builder.WebHost.UseSentry();

    var app = builder.Build();

    // Enable Sentry performance monitoring
    app.UseSentryTracing();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseStaticFiles();

    app.UseRouting();

    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");

    app.Run();
}
catch (Exception e)
{
    SentrySdk.CaptureException(e);
    await SentrySdk.FlushAsync(TimeSpan.FromSeconds(2));
    throw;
}
