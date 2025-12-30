using Sentry.Extensibility;

namespace Sentry.Internal;

/// <summary>
/// Helper class for capturing source code location information for database queries.
/// </summary>
internal static class QuerySourceHelper
{
    /// <summary>
    /// Attempts to add query source information to a span.
    /// </summary>
    /// <param name="span">The span to add source information to.</param>
    /// <param name="options">The Sentry options.</param>
    /// <param name="skipFrames">Number of initial frames to skip (to exclude the helper itself).</param>
    public static void TryAddQuerySource(ISpan span, SentryOptions options, int skipFrames = 0)
    {
        // Check if feature is enabled
        if (!options.EnableDbQuerySource)
        {
            return;
        }

        // Check duration threshold (span must be started)
        if (span.StartTimestamp == null)
        {
            return;
        }

        var duration = DateTimeOffset.UtcNow - span.StartTimestamp.Value;
        if (duration.TotalMilliseconds < options.DbQuerySourceThresholdMs)
        {
            options.LogDebug("Query duration {0}ms is below threshold {1}ms, skipping query source capture",
                duration.TotalMilliseconds, options.DbQuerySourceThresholdMs);
            return;
        }

        try
        {
            // Capture stack trace with file info (requires PDB)
            var stackTrace = new StackTrace(skipFrames, fNeedFileInfo: true);
            var frames = stackTrace.GetFrames();

            if (frames == null || frames.Length == 0)
            {
                options.LogDebug("No stack frames available for query source capture");
                return;
            }

            // Find first "in-app" frame (skip Sentry SDK, EF Core, framework)
            SentryStackFrame? appFrame = null;
            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                if (method == null)
                {
                    continue;
                }

                // Get the declaring type and namespace
                var declaringType = method.DeclaringType;
                var typeNamespace = declaringType?.Namespace;
                var typeName = declaringType?.FullName;

                // Skip Sentry SDK frames
                if (typeNamespace?.StartsWith("Sentry", StringComparison.Ordinal) == true)
                {
                    options.LogDebug("Skipping Sentry SDK frame: {0}", typeName);
                    continue;
                }

                // Skip EF Core frames
                if (typeNamespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true)
                {
                    options.LogDebug("Skipping EF Core frame: {0}", typeName);
                    continue;
                }

                // Skip System.Data frames
                if (typeNamespace?.StartsWith("System.Data", StringComparison.Ordinal) == true)
                {
                    options.LogDebug("Skipping System.Data frame: {0}", typeName);
                    continue;
                }

                // Get file info
                var fileName = frame.GetFileName();
                var lineNumber = frame.GetFileLineNumber();

                // If no file info is available, PDB is likely missing - skip this frame
                if (string.IsNullOrEmpty(fileName))
                {
                    options.LogDebug("No file info for frame {0}, PDB may be missing", typeName ?? method.Name);
                    continue;
                }

                // Create a temporary SentryStackFrame to leverage existing InApp logic
                var module = typeNamespace ?? typeName ?? method.Name;
                var sentryFrame = new SentryStackFrame
                {
                    Module = module,
                    Function = method.Name,
                    FileName = fileName,
                    LineNumber = lineNumber > 0 ? lineNumber : null,
                };

                // Use existing logic to determine if frame is in-app
                sentryFrame.ConfigureAppFrame(options);

                if (sentryFrame.InApp == true)
                {
                    appFrame = sentryFrame;
                    options.LogDebug("Found in-app frame: {0}:{1} in {2}.{3}",
                        fileName, lineNumber, module, method.Name);
                    break;
                }
                else
                {
                    options.LogDebug("Frame not in-app: {0}", typeName ?? method.Name);
                }
            }

            if (appFrame == null)
            {
                options.LogDebug("No in-app frame found for query source");
                return;
            }

            // Set span data with code location attributes
            if (appFrame.FileName != null)
            {
                // Make path relative if possible
                var relativePath = MakeRelativePath(appFrame.FileName, options);
                span.SetExtra("code.filepath", relativePath);
            }

            if (appFrame.LineNumber.HasValue)
            {
                span.SetExtra("code.lineno", appFrame.LineNumber.Value);
            }

            if (appFrame.Function != null)
            {
                span.SetExtra("code.function", appFrame.Function);
            }

            if (appFrame.Module != null)
            {
                span.SetExtra("code.namespace", appFrame.Module);
            }

            options.LogDebug("Added query source: {0}:{1} in {2}",
                appFrame.FileName, appFrame.LineNumber, appFrame.Function);
        }
        catch (Exception ex)
        {
            options.LogError(ex, "Failed to capture query source");
        }
    }

    /// <summary>
    /// Attempts to make a file path relative to the project root.
    /// </summary>
    private static string MakeRelativePath(string filePath, SentryOptions options)
    {
        // Try to normalize path separators
        filePath = filePath.Replace('\\', '/');

        // Try to find common project path indicators and strip them
        // Look for patterns like /src/, /app/, /lib/, etc.
        var segments = new[] { "/src/", "/app/", "/lib/", "/source/", "/code/" };
        foreach (var segment in segments)
        {
            var index = filePath.IndexOf(segment, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var relativePath = filePath.Substring(index + 1);
                options.LogDebug("Made path relative: {0} -> {1}", filePath, relativePath);
                return relativePath;
            }
        }

        // If we can't find a common pattern, try to use just the last few segments
        // to avoid exposing full local filesystem paths
        var parts = filePath.Split('/');
        if (parts.Length > 3)
        {
            var relativePath = string.Join("/", parts.Skip(parts.Length - 3));
            options.LogDebug("Made path relative (last 3 segments): {0} -> {1}", filePath, relativePath);
            return relativePath;
        }

        // Fallback: return as-is
        return filePath;
    }
}
