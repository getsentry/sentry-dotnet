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

        builder.Services.AddQuartz(quartz =>
        {
            quartz.UseSentry(builder.Services, options =>
            {
                var jobKey = new JobKey(nameof(FirstJob));
                quartz.AddJob<FirstJob>(opts => opts.WithIdentity(jobKey));
                quartz.AddTrigger<FirstJob>(opts => opts.ForJob(jobKey).WithIdentity($"{nameof(FirstJob)}-trigger").WithCronSchedule("*/10 * * ? * *"));

                quartz.AddJob<SecondJob>(opts => opts.WithIdentity(jobKey));
                quartz.AddTrigger<SecondJob>(opts => opts.ForJob(jobKey).WithIdentity($"{nameof(SecondJob)}-trigger").WithCronSchedule("*/10 * * ? * *"));

                quartz.AddJob<ThirdJob>(opts => opts.WithIdentity(jobKey));
                quartz.AddTrigger<ThirdJob>(opts => opts.ForJob(jobKey).WithIdentity($"{nameof(ThirdJob)}-trigger").WithCronSchedule("*/10 * * ? * *"));

                if (!builder.Environment.IsProduction())
                {
                    options.EnableUpsertCronMonitor = false;
                }
            });
        });

        var app = builder.Build();

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
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Starting to do some heavy work at: {DateTime.Now}");
        await Task.Delay(1000, cancellationToken);
        Console.WriteLine($"Finished doing some heavy work at: {DateTime.Now}");
        throw new Exception();
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
