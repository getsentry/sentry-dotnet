using System.Text.Json;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Profiling;

internal class ContinuousProfileInfo : ISentryJsonSerializable
{
    public string Version { get; set; } = "2";
    public string Platform { get; set; } = "dotnet";
    public SampleProfile Profile { get; set; } = null!;
    public string? Environment { get; set; }
    public string? Release { get; set; }
    public string? OsName { get; set; }
    public string? OsVersion { get; set; }
    public string? DeviceArchitecture { get; set; }
    public string? RuntimeName { get; set; }
    public string? RuntimeVersion { get; set; }

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteString("version", Version);
        writer.WriteString("platform", Platform);

        if (Environment is not null)
        {
            writer.WriteString("environment", Environment);
        }

        if (Release is not null)
        {
            writer.WriteString("release", Release);
        }

        if (OsName is not null)
        {
            writer.WriteString("os_name", OsName);
        }

        if (OsVersion is not null)
        {
            writer.WriteString("os_version", OsVersion);
        }

        if (DeviceArchitecture is not null)
        {
            writer.WriteString("device_architecture", DeviceArchitecture);
        }

        if (RuntimeName is not null)
        {
            writer.WriteString("runtime_name", RuntimeName);
        }

        if (RuntimeVersion is not null)
        {
            writer.WriteString("runtime_version", RuntimeVersion);
        }

        writer.WritePropertyName("profile");
        Profile.WriteTo(writer, logger);

        writer.WriteEndObject();
    }
} 