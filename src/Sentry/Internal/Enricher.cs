using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Sentry.PlatformAbstractions;
using Sentry.Reflection;
using OperatingSystem = Sentry.Protocol.OperatingSystem;
using Runtime = Sentry.Protocol.Runtime;

namespace Sentry.Internal
{
    internal class Enricher
    {
        private readonly SentryOptions _options;

        private readonly Lazy<Runtime> _runtimeLazy = new(() =>
        {
            var current = PlatformAbstractions.Runtime.Current;
            return new Runtime
            {
                Name = current.Name,
                Version = current.Version,
                RawDescription = current.Raw
            };
        });

        private readonly Lazy<SdkVersion> _sdkVersionLazy =
            new(() => typeof(ISentryClient).Assembly.GetNameAndVersion());

        private readonly Lazy<string?> _releaseLazy = new(ReleaseLocator.GetCurrent);

        public Enricher(SentryOptions options)
        {
            _options = options;
        }

        public void Apply(IEventLike eventLike)
        {
            // Runtime
            if (!eventLike.Contexts.ContainsKey(Runtime.Type))
            {
                eventLike.Contexts[Runtime.Type] = _runtimeLazy.Value;
            }

            // Operating System
            if (!eventLike.Contexts.ContainsKey(OperatingSystem.Type))
            {
                // RuntimeInformation.OSDescription is throwing on Mono 5.12
                if (!PlatformAbstractions.Runtime.Current.IsMono())
                {
                    eventLike.Contexts.OperatingSystem.RawDescription = RuntimeInformation.OSDescription;
                }
            }

            // SDK
            // SDK Name/Version might have be already set by an outer package
            // e.g: ASP.NET Core can set itself as the SDK
            if (eventLike.Sdk.Version is null && eventLike.Sdk.Name is null)
            {
                eventLike.Sdk.Name = Constants.SdkName;
                eventLike.Sdk.Version = _sdkVersionLazy.Value.Version;
            }

            if (_sdkVersionLazy.Value.Version is not null)
            {
                eventLike.Sdk.AddPackage("nuget:" + _sdkVersionLazy.Value.Name, _sdkVersionLazy.Value.Version);
            }

            // Platform
            eventLike.Platform ??= Sentry.Constants.Platform;

            // Release
            eventLike.Release ??= _options.Release ?? _releaseLazy.Value;

            // Environment
            if (string.IsNullOrWhiteSpace(eventLike.Environment))
            {
                var foundEnvironment = EnvironmentLocator.Locate();
                eventLike.Environment = string.IsNullOrWhiteSpace(foundEnvironment)
                    ? string.IsNullOrWhiteSpace(_options.Environment)
                        ? Constants.ProductionEnvironmentSetting
                        : _options.Environment
                    : foundEnvironment;
            }

            // User
            // Report local user if opt-in PII, no user was already set to event and feature not opted-out:
            if (_options.SendDefaultPii)
            {
                if (_options.IsEnvironmentUser && !eventLike.HasUser())
                {
                    eventLike.User.Username = Environment.UserName;
                }

                eventLike.User.IpAddress ??= "{{auto}}";
            }

            //Apply App startup and Boot time
            eventLike.Contexts.App.StartTime ??= ProcessInfo.Instance?.StartupTime;
            eventLike.Contexts.Device.BootTime ??= ProcessInfo.Instance?.BootTime;

            // Default tags
            _options.ApplyDefaultTags(eventLike);
        }
    }
}
