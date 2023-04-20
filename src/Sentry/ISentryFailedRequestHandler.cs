namespace Sentry
{
    internal interface ISentryFailedRequestHandler
    {
        void CaptureEvent(HttpRequestMessage request, HttpResponseMessage response);
    }
}