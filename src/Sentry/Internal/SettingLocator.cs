using System.Diagnostics;
using System.Reflection;
using Sentry.Internal.Extensions;

namespace Sentry.Internal;

/// <summary>
/// Exposes settings that are read from multiple places, such as environment variables, options, attributes, or defaults.
/// </summary>
internal class SettingLocator
{
    private readonly SentryOptions _options;

    public SettingLocator(SentryOptions options)
    {
        _options = options;
    }

    public Assembly? AssemblyForAttributes { get; protected set; } = Assembly.GetEntryAssembly();

    // IMPORTANT: This method has the only usage of Environment.GetEnvironmentVariable in the entire solution.
    // All callers should go through this method.  Overriding this method thus allows one place for mocking environment variables.
    public virtual string? GetEnvironmentVariable(string variable) => Environment.GetEnvironmentVariable(variable);

    /*
     * In all cases, the order of precedence is:
     *  1. A value already assigned to a SentryOptions property
     *  2. A value set in an environment variable
     *  3. A value from an assembly attribute, when applicable
     *  4. A default value, when applicable
     *
     *  Except when already assigned, any non-null value resolved should be assigned to the SentryOptions property.
     */

    public string GetDsn()
    {
        // For DSN only
        // An empty string set on the option should NOT be converted to null because it is used
        // to indicate the the SDK is disabled.

        var dsn = _options.Dsn;
        if (dsn != null)
        {
            return dsn;
        }

        dsn = GetEnvironmentVariable(Constants.DsnEnvironmentVariable)
              ?? AssemblyForAttributes?.GetCustomAttribute<DsnAttribute>()?.Dsn
              ?? Sentry.Constants.DisableSdkDsnValue;

        _options.Dsn = dsn;
        return dsn;
    }

    public string GetEnvironment() => GetEnvironment(true)!;

    public string? GetEnvironment(bool useDefaultIfNotFound)
    {
        var environment = _options.Environment;
        if (!string.IsNullOrWhiteSpace(environment))
        {
            return environment;
        }

        environment = GetEnvironmentVariable(Constants.EnvironmentEnvironmentVariable).NullIfWhitespace();

        if (useDefaultIfNotFound)
        {
            environment ??= Debugger.IsAttached
                ? Constants.DebugEnvironmentSetting
                : Constants.ProductionEnvironmentSetting;
        }
        else if (environment == null)
        {
            return null;
        }

        _options.Environment = environment;
        return environment;
    }

    public string? GetRelease()
    {
        var release = _options.Release;
        if (!string.IsNullOrWhiteSpace(release))
        {
            return release;
        }

        release = GetEnvironmentVariable(Constants.ReleaseEnvironmentVariable).NullIfWhitespace()
                  ?? ApplicationVersionLocator.GetCurrent(AssemblyForAttributes);

        _options.Release = release;
        return release;
    }
}
