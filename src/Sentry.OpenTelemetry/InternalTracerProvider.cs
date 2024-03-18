using OpenTelemetry;
using OpenTelemetry.Trace;
using Sentry.Extensibility;

namespace Sentry.OpenTelemetry;

internal static class InternalTracerProvider
{
    internal static IHub? FallbackHub;
    private static TracerProvider? FallbackTracerProvider;
    private static bool WasInitializedExternally;

    internal static bool InitializedExternally
    {
        get => WasInitializedExternally;
        set
        {
            if (value)
            {
                ClearProvider();
            }

            WasInitializedExternally = value;
        }
    }

    public static void InitializeFallbackTracerProvider(this SentryOptions options)
    {
        try
        {
            ClearProvider();
            // This is a dummy/naive example. It works for something like a Console app. Wiring up an ASP.NET Core app
            // should be done using the OpenTelemetryBuilder extensions... If we knew OpenTelemetry was being used as
            // the trace implementation, we could do something like this in the SentryOptions class, rather than in an
            // extension method, and override it in SentryAspNetCoreOptions etc. to be platform specific.
            options.PostInitCallbacks.Add(hub =>
            {
                FallbackHub = hub;
                FallbackTracerProvider = Sdk.CreateTracerProviderBuilder()
                    .AddSentryInternal(true) // <-- Configure OpenTelemetry to send traces to Sentry
                    .Build();
            });
        }
        catch (InternalTracerProviderException)
        {
            options.LogDebug("OpenTelemetry has been initialized externally: skipping auto-initialization");
        }
    }

    public static void CancelInitialization()
    {
        throw new InternalTracerProviderException();
    }

    public static void ClearProvider()
    {
        FallbackHub = null;
        FallbackTracerProvider?.Dispose();
        FallbackTracerProvider = null;
    }

    internal class InternalTracerProviderException : Exception
    {
        public InternalTracerProviderException(string message) : base(message)
        {
        }

        public InternalTracerProviderException() : base()
        {
        }

        public InternalTracerProviderException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
