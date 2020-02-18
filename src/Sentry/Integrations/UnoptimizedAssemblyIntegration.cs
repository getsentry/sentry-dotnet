using System;
using System.Linq;
using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Reflection;

namespace Sentry.Integrations
{
    /// <summary>
    /// An integration that emits events when detects assemblies compiled for debug.
    /// </summary>
    public class UnoptimizedAssemblyIntegration : ISdkIntegration
    {
        /// <summary>
        /// Registers the integration with the hub and options.
        /// </summary>
        /// <param name="hub">The hub to use to send events.</param>
        /// <param name="options">The options to configure the integration.</param>
        public void Register(IHub hub, SentryOptions options)
        {
            if (options.NotifyUnoptimizedAssembly)
            {
                options.DiagnosticLogger?.LogDebug("Subscribing to AssemblyLoad to detect unoptimized assemblies.");
                // TODO: .NET Core equivalent
                AppDomain.CurrentDomain.AssemblyLoad += (sender, args) =>
                {
                    if (!args.LoadedAssembly.IsOptimized())
                    {
                        CaptureEvent(hub, options, args.LoadedAssembly.FullName);
                        hub.FlushAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
                    }
                };

                var unoptimizedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsOptimized())
                    .Select(a => a.FullName)
                    .ToArray();

                if (unoptimizedAssemblies.Any())
                {
                    CaptureEvent(hub, options, unoptimizedAssemblies);
                }
            }
        }

        private static void CaptureEvent(IHub hub, SentryOptions options, params string[] assemblies)
        {
            if (assemblies.Length > 0)
            {
                options.DiagnosticLogger?.LogInfo("Unoptimized Assembly found. Raising event for {count} assemblies", assemblies.Length);
                using (hub.PushScope())
                {
                    hub.ConfigureScope(s =>
                    {
                        s.SetFingerprint(new[] {"UnoptimizedAssemblyIntegration"});
                        foreach (var asm in assemblies)
                        {
                            options.DiagnosticLogger?.LogInfo("Unoptimized Assembly: {asm}", asm);
                            s.SetExtra("unoptimized-assembly", asm);
                        }
                        s.Level = SentryLevel.Warning;
                    });
                    hub.CaptureMessage("Unoptimized assembly detected.");
                }
            }
        }
    }
}
