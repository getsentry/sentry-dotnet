using System.Diagnostics;

namespace Sentry.Samples.GraphQL.Server;

public static class Telemetry
{
    public const string ServiceName = "Sentry.Samples.GraphQL";
    public static ActivitySource ActivitySource { get; } = new(ServiceName);
}
