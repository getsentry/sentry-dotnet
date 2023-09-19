using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Sentry.Samples.Azure.Functions.Worker;

public class SampleTimerTrigger
{
    [Function(nameof(SampleTimerTrigger))]
    public void Run([TimerTrigger("0 */5 * * * *"/*, RunOnStartup = true*/)] MyInfo myTimer, FunctionContext context)
    {
        var logger = context.GetLogger("SampleTimerTrigger");

        logger.LogInformation("C# Timer trigger function executed at: {Now}", DateTime.Now);
        logger.LogInformation("Next timer schedule at: {NextSchedule}", myTimer.ScheduleStatus.Next);

        BadApple.TimerScenario();
    }
}

public class MyInfo
{
    public MyScheduleStatus ScheduleStatus { get; set; }

    public bool IsPastDue { get; set; }
}

public class MyScheduleStatus
{
    public DateTime Last { get; set; }

    public DateTime Next { get; set; }

    public DateTime LastUpdated { get; set; }
}
