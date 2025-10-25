namespace Sentry.Samples;

public static class SamplesShared
{
#if !SENTRY_DSN_DEFINED_IN_ENV
    /// <summary>
    /// <para>
    /// You must specify a DSN. See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    /// </para>
    /// <para>
    /// On mobile platforms (iOS, Android or MacCatalyst), this should be done in code.
    /// </para>
    /// <para>
    /// On other platforms you can set this in code and it is also possible to set this via an environment variable or
    /// via configuration bindings (e.g. in an app.config or an appsettings.json file).
    /// </para>
    /// </summary>
// #if !CI_BUILD
// #error Sign up for a free Sentry Account and enter your DSN here
// #endif
    public const string Dsn = "ENTER_YOUR_DSN_HERE";
#endif
}
