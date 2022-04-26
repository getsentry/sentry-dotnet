namespace Sentry.Internal
{
    internal interface IHasClientReportRecorder
    {
        IClientReportRecorder ClientReportRecorder { get; }
    }
}
