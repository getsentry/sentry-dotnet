namespace Sentry;

internal interface ISentryFailedRequestHandler
{
    public void HandleResponse(HttpResponseMessage response);
}
