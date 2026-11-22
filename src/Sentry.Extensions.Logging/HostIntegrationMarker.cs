namespace Sentry.Extensions.Logging;

/// <summary>
/// Registered by the host integrations that take precedence over Sentry.Extensions.Hosting, such as Sentry.AspNetCore.
/// UseSentry from Sentry.Extensions.Hosting checks for it and steps aside, so the app runs with that integration's
/// options and a single set of logger providers. It lives in this package because every host integration references
/// it.
/// </summary>
internal sealed class HostIntegrationMarker;
