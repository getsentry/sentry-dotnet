namespace Sentry.GraphQl;

internal class SentryGraphQlHttpFailedRequestHandler : SentryFailedRequestHandler
{
    internal const string MechanismType = "SentryGraphQLHttpFailedRequestHandler";

    internal SentryGraphQlHttpFailedRequestHandler(IHub hub, SentryOptions options)
        : base(hub, options)
    {
    }

    private static readonly Regex ErrorsRegex = new ("(?i)\"errors\"\\s*:\\s*\\[", RegexOptions.Compiled);

    protected internal override void DoEnsureSuccessfulResponse([NotNull]HttpRequestMessage request, [NotNull]HttpResponseMessage response)
    {
        try
        {
            var json = response.Content?.ReadAsJson();
            if (json is { } jsonElement)
            {
                if (jsonElement.TryGetProperty("errors", out var errorsElement))
                {
                    throw new SentryGraphQlHttpFailedRequestException("GraphQL Error");
                }
            }
        }
        catch (Exception exception)
        {
            exception.SetSentryMechanism(MechanismType);

            var @event = new SentryEvent(exception);
            var hint = new Hint(HintTypes.HttpResponseMessage, response);

            // var graphqlInfo = request.Fused<SentryGraphQlRequestInfo>();
            // var sentryRequest = new Requestå
            // {
            //     QueryString = uri?.Query,
            //     Method = request.Method.Method,
            //     ApiTarget = "graphql"
            // };
            //
            // var responseContext = new Response
            // {
            //     StatusCode = (short)response.StatusCode,
            //     BodySize = GetBodySize(response)
            // };
            //
            // if (!_options.SendDefaultPii)
            // {
            //     sentryRequest.Url = uri?.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
            // }
            // else
            // {
            //     sentryRequest.Url = uri?.AbsoluteUri;
            //     sentryRequest.Cookies = request.Headers.GetCookies();
            //     sentryRequest.AddHeaders(request.Headers);
            //     responseContext.Cookies = response.Headers.GetCookies();
            //     responseContext.AddHeaders(response.Headers);
            // }
            //
            // @event.Request = sentryRequest;
            // @event.Contexts[Response.Type] = responseContext;

            Hub.CaptureEvent(@event, hint);
        }
    }
}
