using System;
using System.Threading.Tasks;
using Sentry;

// One of the ways to set your DSN is via an attribute:
// It could be set via AssemblyInfo.cs and patched via CI
[assembly: Dsn("https://key@sentry.io/id")]

namespace Sentry.Samples.Console.Customized
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // With the SDK disabled so the callback is never invoked
            await SentryCore.ConfigureScopeAsync(async scope =>
            {
                // This could be any async I/O operation, like a DB query
                await Task.Yield();
                scope.SetExtra("Key", "Value");
            });

            // Enable the SDK
            SentryCore.Init(o =>
            {
                // Modifications to event before it goes out. Could replace the event altogether
                o.BeforeSend = @event =>
                {
                    // Drop an event altogether:
                    if (@event.Tags.ContainsKey("SomeTag"))
                    {
                        return null;
                    }

                    // Create a totally new event or modify the current one:
                    @event.ServerName = null; // Make sure no ServerName is sent out
                    return @event;
                };
            });

            await SentryCore.ConfigureScopeAsync(async scope =>
            {
                // This could be any async I/O operation, like a DB query
                await Task.Yield(); 
                scope.SetExtra("Key", "Value");
            });

            await SentryCore.CaptureExceptionAsync(new Exception("Something went wrong."));
        }
    }
}
