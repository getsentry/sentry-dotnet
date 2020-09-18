using System;
using System.Net.Http.Headers;
using Sentry.Infrastructure;

namespace Sentry.Internal.Http
{
    internal static class SentryHeaders
    {
        public const string SentryErrorHeader = "X-Sentry-Error";
        public const string SentryAuthHeader = "X-Sentry-Auth";

        /// <summary>
        /// Creates a function that when invoked returns a valid authentication header.
        /// </summary>
        /// <param name="sentryVersion">The sentry version.</param>
        /// <param name="clientVersion">The client version.</param>
        /// <param name="publicKey">The public key.</param>
        /// <param name="secretKey">The secret key.</param>
        /// <param name="clock">The clock.</param>
        public static Action<HttpRequestHeaders> AddSentryAuth(
            int sentryVersion,
            string clientVersion,
            string publicKey,
            string? secretKey,
            ISystemClock? clock = null)
        {
            clock ??= SystemClock.Clock;

            var baseAuthHeader = $"Sentry sentry_version={sentryVersion}," +
               $"sentry_client={clientVersion}," +
               $"sentry_key={publicKey}," +
               (secretKey != null ? $"sentry_secret={secretKey}," : null) +
               "sentry_timestamp=";

            return (headers) => headers.Add(
                SentryAuthHeader,
                baseAuthHeader + clock.GetUtcNow().ToUnixTimeSeconds());
        }
    }
}
