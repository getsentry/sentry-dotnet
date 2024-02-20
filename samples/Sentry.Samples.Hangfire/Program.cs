using Hangfire;
using Hangfire.MemoryStorage;
using Sentry.Hangfire;

namespace Sentry.Samples.Hangfire;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseMemoryStorage(new MemoryStorageOptions())
            .UseSentry() // <- Add Sentry to automatically send check-ins
        );

        builder.Services.AddHangfireServer();

        var app = builder.Build();

        app.UseRouting();

#pragma warning disable ASP0014
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHangfireDashboard();
            endpoints.MapGet("/first", async context =>
            {
                BackgroundJob.Enqueue<FirstJob>(job => job.Execute());
                await context.Response.WriteAsync("Started the first background job!");
            });
            endpoints.MapGet("/second", async context =>
            {
                BackgroundJob.Schedule<SecondJob>(
                    secondJob => secondJob.ExecuteWithException(),
                    TimeSpan.FromSeconds(1));
                await context.Response.WriteAsync("Starting the delayed background job that will throw an exception.");
            });
            endpoints.MapGet("/third", async context =>
            {
                RecurringJob.AddOrUpdate<ThirdJob>(
                    "my_recurring_job",
                    thirdJob => thirdJob.Execute(),
                    Cron.Minutely);
                await context.Response.WriteAsync("Started a recurring background job.");
            });
        });
#pragma warning restore ASP0014

        app.UseHttpsRedirection();

        app.Run();
    }
}

public class FirstJob
{
    // [SentryMonitorSlug("first-job")]
    public void Execute()
    {
        Console.WriteLine($"Starting to do some heavy work at: {DateTime.Now}");
        Task.Delay(1000).Wait();
        Console.WriteLine($"Finished doing some heavy work at: {DateTime.Now}");
    }
}

public class SecondJob
{
    [SentryMonitorSlug("job-that-throws")]
    public void ExecuteWithException()
    {
        Console.WriteLine($"Starting to do some heavy work at: {DateTime.Now}");
        Task.Delay(1000).Wait();
        Console.WriteLine($"Finished doing some heavy work at: {DateTime.Now}");
        throw new Exception();
    }
}

[SentryMonitorSlug("RecurringBackgroundJob")]
public class ThirdJob
{
    public void Execute()
    {
        Console.WriteLine($"Starting to do some heavy work at: {DateTime.Now}");
        Task.Delay(1000).Wait();
        Console.WriteLine($"Finished doing some heavy work at: {DateTime.Now}");
    }
}
