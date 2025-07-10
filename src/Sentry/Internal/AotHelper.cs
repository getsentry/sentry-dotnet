using System;
using Sentry.CompilerServices;
using Sentry.Extensibility;

namespace Sentry.Internal;

internal static class AotHelper
{
    internal const string AvoidAtRuntime = "Non-trimmable code is avoided at runtime";

    internal static bool IsTrimmed { get; }

    static AotHelper()
    {
        IsTrimmed = CheckIsTrimmed();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026: RequiresUnreferencedCode", Justification = AvoidAtRuntime)]
    internal static bool CheckIsTrimmed(IDiagnosticLogger? logger = null)
    {
        if (TryGetBoolean("_IsPublishing", out var isPublishing) && isPublishing)
        {
            logger?.LogDebug("Detected _IsPublishing");
            if (TryGetBoolean("PublishSelfContained", out var selfContained) && selfContained)
            {
                logger?.LogDebug("Detected PublishSelfContained");
                if (TryGetBoolean("PublishTrimmed", out var trimmed))
                {
                    logger?.LogDebug("Detected PublishTrimmed");
                    return trimmed;
                }
            }

            if (TryGetBoolean("PublishAot", out var aot))
            {
                logger?.LogDebug($"Detected PublishAot: {aot}");
                return aot;
            }
        }

        // fallback check
        logger?.LogDebug($"Stacktrace fallback");
        var stackTrace = new StackTrace(false);
        return stackTrace.GetFrame(0)?.GetMethod() is null;
    }

    private static bool TryGetBoolean(string key, out bool value)
    {
        value = false;
        if (BuildProperties.Values?.TryGetValue(key, out string? stringValue) ?? false)
        {
            if (bool.TryParse(stringValue, out var result))
            {
                value = result;
                return true;
            }
        }

        return false;
    }
}
