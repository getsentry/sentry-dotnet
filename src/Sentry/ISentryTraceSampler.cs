namespace Sentry
{
    /// <summary>
    /// Trace sampler.
    /// </summary>
    public interface ISentryTraceSampler
    {
        /// <summary>
        /// Gets the sample rate based on context.
        /// </summary>
        double GetSampleRate(TraceSamplingContext context);
    }
}
