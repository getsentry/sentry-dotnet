var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry(o =>
{
#if !SENTRY_DSN_DEFINED_IN_ENV
    // A DSN is required. You can set here in code, in the SENTRY_DSN environment variable or in your appsettings.json
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    o.Dsn = SamplesShared.Dsn;
#endif
    o.AddProfilingIntegration();
    o.ProfilesSampleRate = 0.1;
    o.TracesSampleRate = 1.0;
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
