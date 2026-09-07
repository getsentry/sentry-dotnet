using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Quartz;
using Sentry.Quartz;

namespace Sentry.Samples.Quartz;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.UseSentry();

        builder.Services.AddQuartzHttpApi();
        builder.Services.AddQuartzDashboard();

        builder.Services.AddQuartz(quartz =>
        {
            quartz.AddSentryScope();
            quartz.AddSentryMetrics();
            quartz.AddSentryCronJobs(options =>
            {
                options.EnableUpsertCronMonitor = builder.Environment.IsProduction();
                options.ConfigureSentryMonitorOptions = (jobDetail, options) =>
                {
                    if (jobDetail.Key.Name == nameof(SecondJob))
                    {
                        options.FailureIssueThreshold = 10;
                        options.RecoveryThreshold = 10;
                    }
                };
            });

            var jobKey1 = new JobKey(nameof(FirstJob));
            quartz.AddJob<FirstJob>(opts => opts.WithIdentity(jobKey1));
            quartz.AddTrigger<FirstJob>(opts => opts.ForJob(jobKey1).WithIdentity($"{nameof(FirstJob)}-trigger").WithCronSchedule("*/10 * * ? * *"));

            var jobKey2 = new JobKey(nameof(SecondJob));
            quartz.AddJob<SecondJob>(opts => opts.WithIdentity(jobKey2));
            quartz.AddTrigger<SecondJob>(opts => opts.ForJob(jobKey2).WithIdentity($"{nameof(SecondJob)}-trigger").WithCronSchedule("*/10 * * ? * *"));

            var jobKey3 = new JobKey(nameof(ThirdJob));
            quartz.AddJob<ThirdJob>(opts => opts.WithIdentity(jobKey3));
            quartz.AddTrigger<ThirdJob>(opts => opts.ForJob(jobKey3).WithIdentity($"{nameof(ThirdJob)}-trigger").WithCronSchedule("*/10 * * ? * *"));

        }).AddQuartzHostedService();

        var app = builder.Build();

        // app.UseAuthentication();
        // app.UseAuthorization();
        app.UseAntiforgery();
        app.MapStaticAssets();

        // No authorization is configured in this sample, so the dashboard is left open. Real
        // applications should call RequireAuthorization() with a policy instead of AllowAnonymous().
        app.MapQuartzHttpApi().AllowAnonymous();
        app.MapQuartzDashboard().AllowAnonymous();

        app.Run();
    }
}

[SentryCronMonitorSlug("first-job")]
public class FirstJob : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Starting to do some heavy work at: {DateTime.Now}");
        await Task.Delay(1000, cancellationToken);
        Console.WriteLine($"Finished doing some heavy work at: {DateTime.Now}");
    }
}

[SentryCronMonitorSlug("job-that-throws")]
public class SecondJob : IJob
{
    private static readonly Random _random = new Random();

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Starting to do some heavy work at: {DateTime.Now}");
        await Task.Delay(1000, cancellationToken);
        Console.WriteLine($"Finished doing some heavy work at: {DateTime.Now}");
        if (_random.Next(100) < 10)
        {
            throw new Exception();
        }
    }
}

[SentryCronMonitorSlug("RecurringBackgroundJob")]
public class ThirdJob : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Starting to do some heavy work at: {DateTime.Now}");
        await Task.Delay(1000, cancellationToken);
        Console.WriteLine($"Finished doing some heavy work at: {DateTime.Now}");
    }
}
