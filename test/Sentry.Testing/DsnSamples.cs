// ReSharper disable once CheckNamespace
namespace Sentry;

public static class DsnSamples
{
    /// <summary>
    /// This is the main DSN we will test with.
    /// </summary>
    public const string ValidDsn = "https://d4d82fc1c2c4032a83f3a29aa3a3aff@fake-sentry.io:65535/2147483647";

    /// <summary>
    /// Missing ProjectId
    /// </summary>
    public const string InvalidDsn = "https://d4d82fc1c2c4032a83f3a29aa3a3aff@fake-sentry.io:65535/";
}
