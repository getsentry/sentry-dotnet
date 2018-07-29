using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Sentry.Extensibility;
using Sentry.PlatformAbstractions;
using Sentry.Protocol;
using Sentry.Reflection;
using OperatingSystem = Sentry.Protocol.OperatingSystem;
using Runtime = Sentry.Protocol.Runtime;

namespace Sentry.Internal
{
    internal class MainSentryEventProcessor : ISentryEventProcessor
    {
        private readonly Lazy<string> _release = new Lazy<string>(ReleaseLocator.GetCurrent);
        private readonly Lazy<string> _environment = new Lazy<string>(EnvironmentLocator.GetCurrent);
        private readonly Lazy<Runtime> _runtime = new Lazy<Runtime>(() =>
        {
           var current = PlatformAbstractions.Runtime.Current;
           return current != null
                  ? new Runtime
                  {
                      Name = current.Name,
                      Version = current.Version,
                      RawDescription = current.Raw
                  }
                  : null;
        });

        private static readonly (string Name, string Version) NameAndVersion
            = typeof(ISentryClient).Assembly.GetNameAndVersion();

        private readonly SentryOptions _options;

        internal string Release => _release.Value;
        internal string Environment => _environment.Value;
        internal Runtime Runtime => _runtime.Value;
        public MainSentryEventProcessor(SentryOptions options)
        {
            Debug.Assert(options != null);
            _options = options;
        }
        public SentryEvent Process(SentryEvent @event)
        {
            if (!@event.Contexts.ContainsKey(Runtime.Type))
            {
                @event.Contexts[Runtime.Type] = Runtime;
            }

            if (!@event.Contexts.ContainsKey(OperatingSystem.Type))
            {
                // RuntimeInformation.OSDescription is throwing on Mono 5.12
                if (!PlatformAbstractions.Runtime.Current.IsMono())
                {
                    @event.Contexts.OperatingSystem.RawDescription = RuntimeInformation.OSDescription;
                }
            }

            @event.Platform = Protocol.Constants.Platform;

            // An integration (e.g: ASP.NET Core) can set itself as the SDK
            // Else, it's the base package: Sentry
            if (@event.Sdk.Name == null || @event.Sdk.Version == null)
            {
                @event.Sdk.Name = Constants.SdkName;
                @event.Sdk.Version = NameAndVersion.Version;
            }

            if (@event.Level == null)
            {
                @event.Level = SentryLevel.Error;
            }

            if (@event.Release == null)
            {
                @event.Release = _options.Release ?? Release;
            }

            if (@event.Environment == null)
            {
                @event.Environment = _options.Environment ?? Environment;
            }

            if (@event.Exception != null)
            {
                // Depends on Options instead of the processors to allow application adding new processors
                // after the SDK is initialized. Useful for example once a DI container is up
                foreach (var processor in _options.GetAllExceptionProcessors())
                {
                    processor.Process(@event.Exception, @event);
                }
            }

            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                var asmName = assembly.GetName();
                builder[asmName.Name] = asmName.Version.ToString();
            }

            @event.InternalModules = builder.ToImmutable();

            return @event;
        }
    }
}
