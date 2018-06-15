using Sentry.Protocol;

namespace Sentry.Internal
{
    internal static class ContextsExtension
    {
        public static void Introspect(this Contexts contexts)
        {
            var runtime = PlatformAbstractions.Runtime.Current;
            if (runtime != null)
            {
                contexts.Runtime.Name = runtime.Name;
                contexts.Runtime.Version = runtime.Version;
                contexts.Runtime.RawDescription = runtime.Raw;
            }
        }
    }
}
