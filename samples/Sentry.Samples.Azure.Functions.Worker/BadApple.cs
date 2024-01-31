namespace Sentry.Samples.Azure.Functions.Worker;

internal static class BadApple
{
    public static void HttpScenario()
    {
        throw new Exception("Throwing from HTTP trigger");
    }

    public static void TimerScenario()
    {
        throw new Exception("Throwing from Timer trigger");
    }
}
