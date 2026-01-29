using Sentry.Extensibility;

namespace Sentry;

/// <summary>
/// Internal non-generic representation of <see cref="SentryMetric{T}"/>.
/// </summary>
internal interface ISentryMetric
{
    /// <inheritdoc cref="ISentryJsonSerializable.WriteTo(Utf8JsonWriter, IDiagnosticLogger)" />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger);
}
