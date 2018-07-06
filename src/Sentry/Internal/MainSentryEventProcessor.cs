using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Reflection;

namespace Sentry.Internal
{
    internal class MainSentryEventProcessor : ISentryEventProcessor
    {
        internal static readonly Lazy<string> Release = new Lazy<string>(ReleaseLocator.GetCurrent);

        private static readonly (string Name, string Version) NameAndVersion
            = typeof(ISentryClient).Assembly.GetNameAndVersion();

        private readonly SentryOptions _options;

        public MainSentryEventProcessor(SentryOptions options)
        {
            Debug.Assert(options != null);
            _options = options;
        }
        public void Process(SentryEvent @event)
        {
            @event.Platform = Constants.Platform;

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
                @event.Release = _options.Release ?? Release.Value;
            }

            if (@event.Exception != null)
            {
                // Depends on Options instead of the processors to allow application adding new processors
                // after the SDK is initialized. Useful for example once a DI container is up
                foreach (var processor in _options.GetExceptionProcessors())
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
        }
    }
}
