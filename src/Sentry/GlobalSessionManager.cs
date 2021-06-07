using System;
using System.IO;
using System.Threading;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry
{
    // AKA client mode
    internal class GlobalSessionManager : ISessionManager
    {
        private readonly object _lock = new();
        private readonly SentryOptions _options;

        private string? _cachedInstallationId;
        private Session? _currentSession;

        public GlobalSessionManager(SentryOptions options)
        {
            _options = options;
        }

        private string GetInstallationId()
        {
            // Installation ID could have already been resolved by this point.
            // Try to retrieve it from cache to avoid expensive I/O and locking.
            if (!string.IsNullOrWhiteSpace(_cachedInstallationId))
            {
                return _cachedInstallationId;
            }

            // Resolve installation ID in a locked manner to avoid race conditions.
            // Note: in the future, this probably has to be synchronized across multiple processes too.
            lock (_lock)
            {
                // We may have acquired the lock after another thread has already resolved
                // installation ID, so check the cache one more time.
                if (!string.IsNullOrWhiteSpace(_cachedInstallationId))
                {
                    return _cachedInstallationId;
                }

                // Store in cache directory or fall back to appdata
                var directoryPath = !string.IsNullOrWhiteSpace(_options.CacheDirectoryPath)
                    ? _options.CacheDirectoryPath
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sentry");

                Directory.CreateDirectory(directoryPath);

                var filePath = Path.Combine(directoryPath, ".installation");

                // Read installation ID stored in a file
                try
                {
                    return _cachedInstallationId = File.ReadAllText(filePath);
                }
                catch (FileNotFoundException)
                {
                }

                // Generate new installation ID and store it in a file
                var id = Guid.NewGuid().ToString();
                File.WriteAllText(filePath, id);

                _options.DiagnosticLogger?.LogDebug(
                    "Saved installation ID '{0}' to file '{1}'.",
                    id, filePath
                );

                return _cachedInstallationId = id;
            }
        }

        public Session? StartSession()
        {
            // Extract release
            var release = ReleaseLocator.Resolve(_options);
            if (string.IsNullOrWhiteSpace(release))
            {
                // Release health without release is just health (useless)
                _options.DiagnosticLogger?.LogError(
                    "Failed to start a session because there is no release information."
                );

                return null;
            }

            // Extract other parameters
            var environment = EnvironmentLocator.Resolve(_options);
            var distinctId = GetInstallationId();

            // Create new session
            var session = new Session(distinctId, release, environment);

            // Set new session and check whether we ended up overwriting an active one in the process
            var previousSession = Interlocked.Exchange(ref _currentSession, session);
            if (previousSession is not null)
            {
                _options.DiagnosticLogger?.LogWarning(
                    "Starting a new session while an existing one is still active."
                );

                // End previous session
                EndSession(previousSession, SessionEndStatus.Exited);
            }

            _options.DiagnosticLogger?.LogInfo(
                "Started new session (SID: {0}; DID: {1}).",
                session.Id, session.DistinctId
            );

            return session;
        }

        public void ReportError()
        {
            if (_currentSession is { } session)
            {
                session.ReportError();
            }
            else
            {
                _options.DiagnosticLogger?.LogError(
                    "Failed to report an error on a session because there is none active."
                );
            }
        }

        private Session EndSession(Session session, SessionEndStatus status)
        {
            session.End(status);

            _options.DiagnosticLogger?.LogInfo(
                "Ended session (SID: {0}; DID: {1}) with state '{2}'.",
                session.Id, session.DistinctId, status
            );

            return session;
        }

        public Session? EndSession(SessionEndStatus status)
        {
            var session = Interlocked.Exchange(ref _currentSession, null);
            if (session is null)
            {
                _options.DiagnosticLogger?.LogError(
                    "Failed to end session because there is none active."
                );

                return null;
            }

            return EndSession(session, status);
        }
    }
}
