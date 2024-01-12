using Hangfire;
using Hangfire.MemoryStorage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage(new MemoryStorageOptions()));

builder.Services.AddHangfireServer();

var app = builder.Build();

app.UseRouting();

#pragma warning disable ASP0014
app.UseEndpoints(endpoints =>
{
    endpoints.MapHangfireDashboard();
});
#pragma warning restore ASP0014

app.UseHttpsRedirection();

app.Run();
