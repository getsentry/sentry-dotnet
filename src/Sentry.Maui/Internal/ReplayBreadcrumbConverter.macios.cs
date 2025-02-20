namespace Sentry;

internal class ReplayBreadcrumbConverter : Sentry.CocoaSdk.SentryReplayBreadcrumbConverter
{
    public override Sentry.CocoaSdk.SentryRRWebEvent? ConvertFrom(Sentry.CocoaSdk.SentryBreadcrumb breadcrumb)
    {
        if (breadcrumb.Timestamp is null)
        {
            return null;
        }

        if (string.Equals(breadcrumb.Category, "touch", StringComparison.OrdinalIgnoreCase))
        {
            return Sentry.CocoaSdk.SentrySessionReplayIntegration.CreateBreadcrumbwithTimestamp(
                breadcrumb.Timestamp,
                breadcrumb.Category,
                breadcrumb.Message,
                breadcrumb.Level,
                breadcrumb.Data
                );
        }

        return null;
    }
}