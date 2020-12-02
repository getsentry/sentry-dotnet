namespace Sentry
{
    public interface ISentryTraceSampler
    {
        double GetSampleRate(TraceSamplingContext context);
    }
}
