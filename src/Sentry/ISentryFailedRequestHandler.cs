namespace Sentry;

internal interface ISentryFailedRequestHandler
{
    void HandleResponse(HttpResponseMessage response);
}
