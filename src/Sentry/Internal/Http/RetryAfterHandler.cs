using System.Net;
using System.Net.Http;
using Sentry.Infrastructure;

namespace Sentry.Internal.Http;

/// <summary>
/// Retry After Handler which short-circuit requests following an HTTP 429.
/// </summary>
/// <seealso href="https://tools.ietf.org/html/rfc6585#section-4" />
/// <seealso href="https://develop.sentry.dev/sdk/overview/#writing-an-sdk"/>
/// <inheritdoc />
internal class RetryAfterHandler : DelegatingHandler
{
    private readonly ISystemClock _clock;

    private const HttpStatusCode TooManyRequests = (HttpStatusCode)429;
    internal static readonly TimeSpan DefaultRetryAfterDelay = TimeSpan.FromSeconds(60);

    private long _retryAfterUtcTicks;
    internal long RetryAfterUtcTicks => _retryAfterUtcTicks;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryAfterHandler"/> class.
    /// </summary>
    /// <param name="innerHandler">The inner handler which is responsible for processing the HTTP response messages.</param>
    public RetryAfterHandler(HttpMessageHandler innerHandler)
        : this(innerHandler, SystemClock.Clock)
    { }

    internal RetryAfterHandler(HttpMessageHandler innerHandler, ISystemClock clock)
        : base(innerHandler)
        => _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    /// <summary>
    /// Sends an HTTP request to the inner handler while verifying the Response status code for HTTP 429.
    /// </summary>
    /// <param name="request">The HTTP request message to send to the server.</param>
    /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
    /// <returns>
    /// The task object representing the asynchronous operation.
    /// </returns>
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var retryAfter = Interlocked.CompareExchange(ref _retryAfterUtcTicks, 0, 0);
        if (retryAfter != 0)
        {
            if (retryAfter > _clock.GetUtcNow().Ticks)
            {
                // Important: don't reuse the same HttpResponseMessage in multiple requests!
                // https://github.com/getsentry/sentry-dotnet/issues/800
                return new HttpResponseMessage(TooManyRequests);
            }

            _ = Interlocked.Exchange(ref _retryAfterUtcTicks, 0);
        }

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == TooManyRequests)
        {
            var retryAfterTimestamp = GetRetryAfterTimestamp(response);
            _ = Interlocked.Exchange(ref _retryAfterUtcTicks, retryAfterTimestamp.UtcTicks);
        }

        return response;
    }

    private DateTimeOffset GetRetryAfterTimestamp(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter != null)
        {
            if (response.Headers.RetryAfter.Delta is { } delta)
            {
                return _clock.GetUtcNow() + delta;
            }

            if (response.Headers.RetryAfter.Date is { } date)
            {
                return date;
            }
        }

        // Sentry was sending floating point numbers which are not handled by RetryConditionHeaderValue
        // To be compatible with older versions of sentry on premise: https://github.com/getsentry/sentry/issues/7919
        if (response.Headers.TryGetValues("Retry-After", out var values)
            && double.TryParse(values.FirstOrDefault(), out var retryAfterSeconds))
        {
            return _clock.GetUtcNow().AddTicks((long)(retryAfterSeconds * TimeSpan.TicksPerSecond));
        }

        // No retry header was sent. Use the default retry delay.
        return _clock.GetUtcNow() + DefaultRetryAfterDelay;
    }
}
