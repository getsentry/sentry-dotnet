// ReSharper disable once CheckNamespace
namespace Sentry;

public static class DsnSamples
{
    /// <summary>
    /// This DSN is well-formed and will pass tests.
    /// It has a fake domain name, so will cause a DNS failure if trying to actually send data.
    /// </summary>
    public const string ValidDsn = "https://d4d82fc1c2c4032a83f3a29aa3a3aff@fake-sentry.io:65535/2147483647";

    /// <summary>
    /// This DSN is malformed.  It is missing a Sentry Project ID.
    /// </summary>
    public const string InvalidDsn = "https://d4d82fc1c2c4032a83f3a29aa3a3aff@fake-sentry.io:65535/";

    /// <summary>
    /// This DSN is well-formed and will pass tests.
    /// However, it includes a secret, which is no longer required by Sentry.
    /// It also has a fake domain name, so will cause a DNS failure if trying to actually send data.
    /// </summary>
    [Obsolete("Sentry has dropped the use of secrets in DSNs.  Only use this when testing for backwards compatibility.")]
    public const string ValidDsnWithSecret = "https://d4d82fc1c2c4032a83f3a29aa3a3aff:ed0a8589a0bb4d4793ac4c70375f3d65@fake-sentry.io:65535/2147483647";
}
