using System;
using Android.OS;
using Android.Runtime;
using global::Java.Lang;
using Sentry.Internal;
using Sentry.Protocol;
using JavaObject = global::Java.Lang.Object;

namespace Sentry
{
    public static partial class SentrySdk
    {
        public static IDisposable Init(
            global::Android.Content.Context context,
            Action<SentryOptions>? configureOptions)
        {
            var options = new SentryOptions();
            // TODO: Pause/Resume
            options.AutoSessionTracking = true;
            options.IsGlobalModeEnabled = true;
            options.AddEventProcessor(new DelegateEventProcessor(evt =>
            {
#pragma warning disable 618
                evt.Contexts.Device.Architecture = Build.CpuAbi;
#pragma warning restore 618
                evt.Contexts.Device.Manufacturer = Build.Manufacturer;
                return evt;
            }));

            configureOptions?.Invoke(options);

            Sentry.Android.SentryAndroid.Init(context, new JavaLogger(options),
                new ConfigureOption(o =>
                {
                    o.Dsn = options.Dsn;
                }));

            options.CrashedLastRun = () => Sentry.Java.Sentry.IsCrashedLastRun()?.BooleanValue() is true;

            AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironment_UnhandledExceptionRaiser;

            return Init(options);
        }

        private static void AndroidEnvironment_UnhandledExceptionRaiser(object? _, RaiseThrowableEventArgs e)
        {
            e.Exception.Data[Mechanism.HandledKey] = e.Handled;
            e.Exception.Data[Mechanism.MechanismKey] = "UnhandledExceptionRaiser";
            CaptureException(e.Exception);
            if (!e.Handled)
            {
                Close();
            }
        }

        internal class ConfigureOption : JavaObject, global::Sentry.Java.Sentry.IOptionsConfiguration
        {
            private readonly Action<Sentry.Android.SentryAndroidOptions> _configureOptions;

            public ConfigureOption(Action<Sentry.Android.SentryAndroidOptions> configureOptions) => _configureOptions = configureOptions;

            public void Configure(JavaObject optionsObject)
            {
                var options = (Sentry.Android.SentryAndroidOptions)optionsObject;
                _configureOptions(options);
            }
        }
    }

    internal class JavaLogger : JavaObject, global::Sentry.Java.ILogger
    {
        private readonly SentryOptions _options;

        public JavaLogger(SentryOptions options) => _options = options;

        public void Log(global::Sentry.Java.SentryLevel level, string message, JavaObject[]? args)
        {
            // TODO:
            _options.DiagnosticLogger?.Log(SentryLevel.Debug, message, null, Array.Empty<object>());
        }

        public void Log(global::Sentry.Java.SentryLevel level, string message,
            global::Java.Lang.Throwable? throwable)
        {
            // TODO:
            _options.DiagnosticLogger?.Log(SentryLevel.Debug, message, throwable, Array.Empty<object>());
        }

        public void Log(global::Sentry.Java.SentryLevel level, Throwable? throwable, string message, params JavaObject[]? args)
        {
            // TODO:
            _options.DiagnosticLogger?.Log(SentryLevel.Debug, message, throwable, Array.Empty<object>());
        }

        public bool IsEnabled(global::Sentry.Java.SentryLevel? level)
        {
            // TODO: map level
            return _options.DiagnosticLogger?.IsEnabled(SentryLevel.Debug) == true;
        }
    }
}
