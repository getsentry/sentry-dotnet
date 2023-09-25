// ReSharper disable once CheckNamespace
namespace Sentry.Internal.OpenTelemetry;

internal static class StatusTags
{
    // See https://github.com/open-telemetry/opentelemetry-dotnet/blob/dacc532d51ca0f3775160b84fa6d7d9403a8ccde/src/Shared/StatusHelper.cs#L26
    public const string UnsetStatusCodeTagValue = "UNSET";
    public const string OkStatusCodeTagValue = "OK";
    public const string ErrorStatusCodeTagValue = "ERROR";
}
