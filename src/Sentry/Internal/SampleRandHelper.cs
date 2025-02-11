namespace Sentry.Internal;

internal static class SampleRandHelper
{
    internal static double GenerateSampleRand(string traceId)
        => new Random(FnvHash.ComputeHash(traceId)).NextDouble();

    internal static bool IsSampled(double sampleRand, double rate) => rate switch
    {
        >= 1 => true,
        <= 0 => false,
        _ => sampleRand < rate
    };

}
