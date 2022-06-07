using Sentry.Extensibility;

namespace Sentry.Android
{
    internal class BeforeBreadcrumbCallback : JavaObject, Java.SentryOptions.IBeforeBreadcrumbCallback
    {
        private readonly Func<Breadcrumb, Breadcrumb?> _beforeBreadcrumb;
        private readonly IDiagnosticLogger? _logger;
        private readonly Java.SentryOptions _javaOptions;

        public BeforeBreadcrumbCallback(
            Func<Breadcrumb, Breadcrumb?> beforeBreadcrumb,
            IDiagnosticLogger? logger,
            Java.SentryOptions javaOptions)
        {
            _beforeBreadcrumb = beforeBreadcrumb;
            _logger = logger;
            _javaOptions = javaOptions;
        }

        public Java.Breadcrumb? Execute(Java.Breadcrumb b, Java.Hint h)
        {
            // Note: Hint is unused due to:
            // https://github.com/getsentry/sentry-dotnet/issues/1469

            var breadcrumb = b.ToBreadcrumb(_javaOptions);
            var result = _beforeBreadcrumb.Invoke(breadcrumb);
            return result?.ToJavaBreadcrumb(_logger, _javaOptions);
        }
    }
}
