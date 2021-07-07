﻿using System;
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
        private const string PersistedSessionFileName = ".session";

        private readonly object _installationIdLock = new();

        private readonly ISystemClock _clock;
        private readonly SentryOptions _options;

        private readonly string? _persistanceDirectoryPath;

        private string? _resolvedInstallationId;
        private Session? _currentSession;

        // Internal for testing
        internal Session? CurrentSession => _currentSession;

        public bool IsSessionActive => _currentSession is not null;

        public GlobalSessionManager(SentryOptions options, ISystemClock clock)
        {
            _options = options;
            _clock = clock;

            // TODO: session file should really be process-isolated, but we
            // don't have a proper mechanism for that right now.
            _persistanceDirectoryPath = options.TryGetDsnSpecificCacheDirectoryPath();
        }

        public GlobalSessionManager(SentryOptions options)
            : this(options, SystemClock.Clock)
        {
        }

        private string? TryGetPersistentInstallationId()
        {
            try
            {
                var directoryPath =
                    _options.TryGetDsnSpecificCacheDirectoryPath() ??
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sentry");

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
            if (!string.IsNullOrWhiteSpace(_resolvedInstallationId))
            {
                return _resolvedInstallationId;
            }

            // Resolve installation ID in a locked manner to guarantee consistency because ID can be non-deterministic.
            // Note: in the future, this probably has to be synchronized across multiple processes too.
            lock (_installationIdLock)
            {
                // We may have acquired the lock after another thread has already resolved
                // installation ID, so check the cache one more time before proceeding with I/O.
                if (!string.IsNullOrWhiteSpace(_resolvedInstallationId))
                {
                    return _resolvedInstallationId;
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

                return _resolvedInstallationId = id;
            }
        }

        private void PersistSession(SessionUpdate update, bool isPaused = false)
        {
            _options.DiagnosticLogger?.LogDebug("Persisting session (SID: '{0}') to a file.", update.Id);

            if (string.IsNullOrWhiteSpace(_persistanceDirectoryPath))
            {
                _options.DiagnosticLogger?.LogDebug("Persistance directory is not set, returning.");
                return;
            }

            try
            {
                Directory.CreateDirectory(_persistanceDirectoryPath);

                _options.DiagnosticLogger?.LogDebug(
                    "Created persistance directory for session file '{0}'.",
                    _persistanceDirectoryPath
                );

                var filePath = Path.Combine(_persistanceDirectoryPath, PersistedSessionFileName);
                update.WriteToFile(filePath);

                _options.DiagnosticLogger?.LogInfo(
                    "Persisted session to a file '{0}'.",
                    filePath
                );
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError(
                    "Failed to persist session on the file system.",
                    ex
                );
            }
        }

        private void DeletePersistedSession()
        {
            _options.DiagnosticLogger?.LogDebug("Deleting persisted session file.");

            if (string.IsNullOrWhiteSpace(_persistanceDirectoryPath))
            {
                _options.DiagnosticLogger?.LogDebug("Persistance directory is not set, returning.");
                return;
            }

            try
            {
                var filePath = Path.Combine(_persistanceDirectoryPath, PersistedSessionFileName);

                // Try to log the contents of the session file before we delete it
                if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Debug) ?? false)
                {
                    try
                    {
                        _options.DiagnosticLogger?.LogDebug(
                            "Deleting persisted session file with contents: {0}",
                            File.ReadAllText(filePath)
                        );
                    }
                    catch (Exception ex)
                    {
                        _options.DiagnosticLogger?.LogError(
                            "Failed to read the contents of persisted session file '{0}'.",
                            ex,
                            filePath
                        );
                    }
                }

                File.Delete(filePath);

                _options.DiagnosticLogger?.LogInfo(
                    "Deleted persisted session file '{0}'.",
                    filePath
                );
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError(
                    "Failed to delete persisted session from the file system.",
                    ex
                );
            }
        }

        public SessionUpdate? TryRecoverPersistedSession()
        {
            _options.DiagnosticLogger?.LogDebug("Attempting to recover persisted session from file.");

            if (string.IsNullOrWhiteSpace(_persistanceDirectoryPath))
            {
                _options.DiagnosticLogger?.LogDebug("Persistance directory is not set, returning.");
                return null;
            }

            try
            {
                var filePath = Path.Combine(_persistanceDirectoryPath, PersistedSessionFileName);
                var recoveredUpdate = SessionUpdate.FromJson(Json.Load(filePath));

                // Switch status to abnormal and initial to false
                // TODO: crashed for paused sessions
                return new SessionUpdate(recoveredUpdate, false, SessionEndStatus.Abnormal);
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError(
                    "Failed to recover persisted session from the file system",
                    ex
                );

                return null;
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

            var update = session.CreateUpdate(true, _clock.GetUtcNow());

            PersistSession(update);

            return update;
        }

        private SessionUpdate EndSession(Session session, DateTimeOffset timestamp, SessionEndStatus status)
        {
            _options.DiagnosticLogger?.LogInfo(
                "Ended session (SID: {0}; DID: {1}) with status '{2}'.",
                session.Id, session.DistinctId, status
            );

            var update = session.CreateUpdate(false, timestamp, status);

            DeletePersistedSession();

            return update;
        }

        public SessionUpdate? EndSession(DateTimeOffset timestamp, SessionEndStatus status)
        {
            var session = Interlocked.Exchange(ref _currentSession, null);
            if (session is null)
            {
                _options.DiagnosticLogger?.LogDebug(
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
