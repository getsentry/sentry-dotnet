using System;
using System.Diagnostics;
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
    public class Dsn
    {
        private readonly string _dsn;

        /// <summary>
        /// The project ID which the authenticated user is bound to.
        /// </summary>
        public string ProjectId { get; }
        /// <summary>
        /// An optional path of which Sentry is hosted
        /// </summary>
        public string Path { get; }
        /// <summary>
        /// The optional secret key to authenticate the SDK.
        /// </summary>
        public string SecretKey { get; }
        /// <summary>
        /// The required public key to authenticate the SDK.
        /// </summary>
        public string PublicKey { get; }
        /// <summary>
        /// The URI used to communicate with Sentry
        /// </summary>
        public Uri SentryUri { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dsn"/> class.
        /// </summary>
        /// <param name="dsn">The DSN in the format: {PROTOCOL}://{PUBLIC_KEY}@{HOST}/{PATH}{PROJECT_ID}</param>
        /// <remarks>
        /// A legacy DSN containing a secret will also be accepted: {PROTOCOL}://{PUBLIC_KEY}:{SECRET_KEY}@{HOST}/{PATH}{PROJECT_ID}
        /// </remarks>
        public Dsn(string dsn)
        {
            var parsed = Parse(dsn, throwOnError: true);
            Debug.Assert(parsed != null, "Parse should throw instead of returning null!");

            _dsn = parsed.Item1;
            ProjectId = parsed.Item2;
            Path = parsed.Item3;
            SecretKey = parsed.Item4;
            PublicKey = parsed.Item5;
            SentryUri = parsed.Item6;
        }

        private Dsn(string dsn, string projectId, string path, string secretKey, string publicKey, Uri sentryUri)
        {
            _dsn = dsn;
            ProjectId = projectId;
            Path = path;
            SecretKey = secretKey;
            PublicKey = publicKey;
            SentryUri = sentryUri;
        }

        /// <summary>
        /// Determines whether the specified DSN means a disabled SDK or not..
        /// </summary>
        /// <param name="dsn">The DSN.</param>
        /// <returns>
        ///   <c>true</c> if the string represents a disabled DSN; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDisabled(string dsn) =>
            Constants.DisableSdkDsnValue.Equals(dsn, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Tries to parse the string into a <see cref="Dsn"/>
        /// </summary>
        /// <param name="dsn">The string to attempt parsing.</param>
        /// <param name="finalDsn">The <see cref="Dsn"/> when successfully parsed.</param>
        /// <returns><c>true</c> if the string is a valid <see cref="Dsn"/> as was successfully parsed. Otherwise, <c>false</c>.</returns>
        public static bool TryParse(string dsn, out Dsn finalDsn)
        {
            try
            {
                var parsed = Parse(dsn, throwOnError: false);
                if (parsed == null)
                {
                    finalDsn = null;
                    return false;
                }

                finalDsn = new Dsn(parsed.Item1, parsed.Item2, parsed.Item3, parsed.Item4, parsed.Item5, parsed.Item6);
                return true;
            }
            catch
            {
                // Parse should not throw though!
                finalDsn = null;
                return false;
            }
        }

        private static Tuple<string, string, string, string, string, Uri> Parse(string dsn, bool throwOnError)
        {
            Uri uri;
            if (throwOnError)
            {
                uri = new Uri(dsn); // Throws the UriFormatException one would expect
            }
            else if (Uri.TryCreate(dsn, UriKind.Absolute, out var parsedUri))
            {
                uri = parsedUri;
            }
            else
            {
                return null;
            }

            // uri.UserInfo returns empty string instead of null when no user info data is provided
            if (string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                if (throwOnError)
                {
                    throw new ArgumentException("Invalid DSN: No public key provided.");
                }
                return null;
            }

            var keys = uri.UserInfo.Split(':');
            var publicKey = keys[0];
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                if (throwOnError)
                {
                    throw new ArgumentException("Invalid DSN: No public key provided.");
                }
                return null;
            }

            string secretKey = null;
            if (keys.Length > 1)
            {
                secretKey = keys[1];
            }

            var path = uri.AbsolutePath.Substring(0, uri.AbsolutePath.LastIndexOf('/'));
            var projectId = uri.AbsoluteUri.Substring(uri.AbsoluteUri.LastIndexOf('/') + 1);

            if (string.IsNullOrWhiteSpace(projectId))
            {
                if (throwOnError)
                {
                    throw new ArgumentException("Invalid DSN: A Project Id is required.");
                }
                return null;
            }

            var builder = new UriBuilder
            {
                Scheme = uri.Scheme,
                Host = uri.DnsSafeHost,
                Port = uri.Port,
                Path = $"{path}/api/{projectId}/store/"
            };

            return Tuple.Create(dsn, projectId, path, secretKey, publicKey, builder.Uri);
        }

        /// <summary>
        /// The original DSN string used to create this instance
        /// </summary>
        public override string ToString() => _dsn;
    }
}
