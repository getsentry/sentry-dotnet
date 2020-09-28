using System;
using Sentry.Protocol;

// ReSharper disable once CheckNamespace
namespace Sentry
{
    /// <summary>
    /// The Data Source Name of a given project in Sentry.
    /// </summary>
    /// <remarks>
    /// <see href="https://docs.sentry.io/quickstart/#configure-the-dsn"/>
    /// </remarks>
    public sealed class Dsn
    {
        public string OriginalString { get; }

        public string ProjectId { get; }

        public string? Path { get; }

        public string? SecretKey { get; }

        public string PublicKey { get; }

        public Uri SentryUri { get; }

        private Dsn(
            string originalString,
            string projectId,
            string? path,
            string? secretKey,
            string publicKey,
            Uri sentryUri)
        {
            OriginalString = originalString;
            ProjectId = projectId;
            Path = path;
            SecretKey = secretKey;
            PublicKey = publicKey;
            SentryUri = sentryUri;
        }

        public override string ToString() => OriginalString;

        internal static bool IsDisabled(string dsn) =>
            Constants.DisableSdkDsnValue.Equals(dsn, StringComparison.OrdinalIgnoreCase);

        internal static Dsn Parse(string dsn)
        {
            var uri = new Uri(dsn);

            // uri.UserInfo returns empty string instead of null when no user info data is provided
            if (string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                throw new ArgumentException("Invalid DSN: No public key provided.");
            }

            var keys = uri.UserInfo.Split(':');
            var publicKey = keys[0];
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                throw new ArgumentException("Invalid DSN: No public key provided.");
            }

            string? secretKey = null;
            if (keys.Length > 1)
            {
                secretKey = keys[1];
            }

            var path = uri.AbsolutePath.Substring(0, uri.AbsolutePath.LastIndexOf('/'));
            var projectId = uri.AbsoluteUri.Substring(uri.AbsoluteUri.LastIndexOf('/') + 1);

            if (string.IsNullOrWhiteSpace(projectId))
            {
                throw new ArgumentException("Invalid DSN: A Project Id is required.");
            }

            var sentryUri = new UriBuilder
            {
                Scheme = uri.Scheme,
                Host = uri.DnsSafeHost,
                Port = uri.Port,
                Path = $"{path}/api/{projectId}/store/"
            }.Uri;

            return new Dsn(
                dsn,
                projectId,
                path,
                secretKey,
                publicKey,
                sentryUri
            );
        }

        internal static Dsn? TryParse(string? dsn)
        {
            if (string.IsNullOrWhiteSpace(dsn))
            {
                return null;
            }

            try
            {
                return Parse(dsn);
            }
            catch
            {
                // Parse should not throw though!
                return null;
            }
        }
    }
}
