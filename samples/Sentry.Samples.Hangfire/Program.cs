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
            .UseSentry()
        );

        builder.Services.AddSingleton<IHostedService, MyJobStarter>();
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
    }
}

public class MyJobStarter : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("MyJobStarter is starting...");

        var jobId = BackgroundJob.Enqueue<MyBackgroundJob>(x => x.Execute());
        Console.WriteLine($"Job Enqueued. JobId: {jobId}");

        // RecurringJob.AddOrUpdate<MyBackgroundJob>(
        //     "my_recurring_job",
        //     x => x.Execute(),
        //     Cron.Minutely);
        //
        // for (var i = 0; i < 100; i++)
        // {
        //     var job = BackgroundJob.Schedule<MyBackgroundJob>(
        //         x => x.Execute(),
        //         TimeSpan.FromSeconds(new Random().Next(1, 100)));
        // }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class MyBackgroundJob
{
    public void Execute()
    {
        Console.WriteLine($"Background task executed at: {DateTime.Now}");
        Task.Delay(2000).Wait();
        Console.WriteLine($"Background task finished at: {DateTime.Now}");
    }
}
