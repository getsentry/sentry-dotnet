namespace Sentry.Extensions.Logging;

/// <summary>
/// Registered by every host integration that adds Sentry logger providers, Sentry.Extensions.Hosting and
/// Sentry.AspNetCore.Blazor.WebAssembly included. Logging.AddSentry() checks for it and disables its own providers,
/// so each log entry is sent once.
/// </summary>
internal sealed class HostLoggerProvidersMarker;
