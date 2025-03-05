using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Sentry JsonSerializable.
/// </summary>
public interface ISentryJsonSerializable
{
    /// <summary>
    /// Writes the object as JSON.
    /// </summary>
    /// <remarks>
    /// Note: this method is meant only for internal use and is exposed due to a language limitation.
    /// Avoid relying on this method in user code.
    /// </remarks>
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger);
}
