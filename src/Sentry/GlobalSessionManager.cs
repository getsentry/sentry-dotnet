using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;

namespace Sentry
{
    // AKA client mode
    internal class GlobalSessionManager : ISessionManager
    {
        private readonly object _installationIdLock = new();

        private readonly ISystemClock _clock;
        private readonly SentryOptions _options;

        private string? _cachedInstallationId;
        private Session? _currentSession;

        // Internal for testing
        internal Session? CurrentSession => _currentSession;

        public GlobalSessionManager(SentryOptions options, ISystemClock clock)
        {
            _options = options;
            _clock = clock;
        }

        public GlobalSessionManager(SentryOptions options)
            : this(options, SystemClock.Clock)
        {
        }

        private string? TryGetPersistentInstallationId()
        {
            try
            {
                var directoryPath = Path.Combine(
                    // Store in cache directory or fall back to appdata
                    !string.IsNullOrWhiteSpace(_options.CacheDirectoryPath)
                        ? _options.CacheDirectoryPath
                        : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    // Put under "Sentry" subdirectory
                    "Sentry"
                );

                Directory.CreateDirectory(directoryPath);

                var filePath = Path.Combine(directoryPath, ".installation");

                // Read installation ID stored in a file
                try
                {
                    return File.ReadAllText(filePath);
                }
                catch (FileNotFoundException)
                {
                    _options.DiagnosticLogger?.LogDebug(
                        "File containing installation ID does not exist ({0}).",
                        filePath
                    );
                }

                // Generate new installation ID and store it in a file
                var id = Guid.NewGuid().ToString();
                File.WriteAllText(filePath, id);

                _options.DiagnosticLogger?.LogDebug(
                    "Saved installation ID '{0}' to file '{1}'.",
                    id, filePath
                );

                return id;
            }
            // If there's no write permission or the platform doesn't support this, we handle
            // and let the next installation id strategy kick in
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError(
                    "Failed to resolve persistent installation ID.",
                    ex
                );

                return null;
            }
        }

        private string? TryGetHardwareInstallationId()
        {
            try
            {
                var installationId = NetworkInterface
                    .GetAllNetworkInterfaces()
                    .Where(nic =>
                        nic.OperationalStatus == OperationalStatus.Up &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .Select(nic => nic.GetPhysicalAddress().ToString())
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(installationId))
                {
                    _options.DiagnosticLogger?.LogError(
                        "Failed to resolve hardware installation ID."
                    );

                    return null;
                }

                return installationId;
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError(
                    "Failed to resolve hardware installation ID.",
                    ex
                );

                return null;
            }
        }

        private string? TryGetInstallationId()
        {
            // Installation ID could have already been resolved by this point
            if (!string.IsNullOrWhiteSpace(_cachedInstallationId))
            {
                return _cachedInstallationId;
            }

            // Resolve installation ID in a locked manner to guarantee consistency because ID can be non-deterministic.
            // Note: in the future, this probably has to be synchronized across multiple processes too.
            lock (_installationIdLock)
            {
                // We may have acquired the lock after another thread has already resolved
                // installation ID, so check the cache one more time before proceeding with I/O.
                if (!string.IsNullOrWhiteSpace(_cachedInstallationId))
                {
                    return _cachedInstallationId;
                }

                var id =
                    TryGetPersistentInstallationId() ??
                    TryGetHardwareInstallationId();

                if (!string.IsNullOrWhiteSpace(id))
                {
                    _options.DiagnosticLogger?.LogDebug(
                        "Resolved installation ID '{0}'.",
                        id
                    );
                }
                else
                {
                    _options.DiagnosticLogger?.LogDebug(
                        "Failed to resolve installation ID."
                    );
                }

                return _cachedInstallationId = id;
            }
        }

        public SessionUpdate? StartSession()
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
            var distinctId = TryGetInstallationId();

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
                EndSession(previousSession, _clock.GetUtcNow(), SessionEndStatus.Exited);
            }

            _options.DiagnosticLogger?.LogInfo(
                "Started new session (SID: {0}; DID: {1}).",
                session.Id, session.DistinctId
            );

            return session.CreateUpdate(true, _clock.GetUtcNow());
        }

        private SessionUpdate EndSession(Session session, DateTimeOffset timestamp, SessionEndStatus status)
        {
            _options.DiagnosticLogger?.LogInfo(
                "Ended session (SID: {0}; DID: {1}) with status '{2}'.",
                session.Id, session.DistinctId, status
            );

            return session.CreateUpdate(false, timestamp, status);
        }

        public SessionUpdate? EndSession(DateTimeOffset timestamp, SessionEndStatus status)
        {
            var session = Interlocked.Exchange(ref _currentSession, null);
            if (session is null)
            {
                _options.DiagnosticLogger?.LogError(
                    "Failed to end session because there is none active."
                );

                return null;
            }

            return EndSession(session, timestamp, status);
        }

        public SessionUpdate? EndSession(SessionEndStatus status) => EndSession(_clock.GetUtcNow(), status);

        public SessionUpdate? ReportError()
        {
            if (_currentSession is { } session)
            {
                session.ReportError();

                // If we already have at least one error reported, the session update is pointless,
                // so don't return anything.
                if (session.ErrorCount > 1)
                {
                    _options.DiagnosticLogger?.LogDebug(
                        "Reported an error on a session that already contains errors. Not creating an update."
                    );

                    return null;
                }

                return session.CreateUpdate(false, _clock.GetUtcNow());
            }

            _options.DiagnosticLogger?.LogDebug(
                "Failed to report an error on a session because there is none active."
            );

            return null;
        }
    }
}
