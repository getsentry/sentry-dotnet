using System.ComponentModel;

namespace Microsoft.Extensions.Logging
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SentryLoggerFactoryExtensions
    {
        public static ILoggerFactory AddSentry(this ILoggerFactory factory)
        {
            factory.AddProvider(new SentryLoggerProvider());
            return factory;
        }
    }
}
