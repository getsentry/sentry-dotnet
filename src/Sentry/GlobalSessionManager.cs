using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry
{
    // AKA client mode
    // https://develop.sentry.dev/sdk/sessions
    internal class GlobalSessionManager : ISessionManager
    {
        private const string PersistedSessionFileName = ".session";

        private readonly object _installationIdLock = new();
        private readonly object _pauseResumeLock = new();

        private readonly SentryOptions _options;
        private readonly ISentryClient _client;
        private readonly IInternalScopeManager _scopeManager;
        private readonly ISystemClock _clock;
        private readonly Func<string, PersistedSessionUpdate> _persistedSessionProvider;

        private readonly string? _persistenceDirectoryPath;

        private string? _resolvedInstallationId;
        private int _isPersistedSessionRecovered;
        private DateTimeOffset? _lastPauseTimestamp;

        // Internal for testing
        private Session? _currentSession;
        internal Session? CurrentSession => _currentSession;

        public GlobalSessionManager(
            SentryOptions options,
            ISentryClient client,
            IInternalScopeManager scopeManager,
            ISystemClock? clock = null,
            Func<string, PersistedSessionUpdate>? persistedSessionProvider = null)
        {
            _options = options;
            _client = client;
            _scopeManager = scopeManager;
            _clock = clock ?? SystemClock.Clock;
            _persistedSessionProvider =
                persistedSessionProvider
                ?? (filePath => PersistedSessionUpdate.FromJson(Json.Load(filePath)));

            // TODO: session file should really be process-isolated, but we
            // don't have a proper mechanism for that right now.
            _persistenceDirectoryPath = options.TryGetDsnSpecificCacheDirectoryPath();
        }

        private string? TryGetPersistentInstallationId()
        {
            try
            {
                var directoryPath =
                    _persistenceDirectoryPath
                    ?? Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Sentry",
                        _options.Dsn!.GetHashString());

                Directory.CreateDirectory(directoryPath);

                _options.DiagnosticLogger?.LogDebug(
                    "Created directory for installation ID file ({0}).",
                    directoryPath);

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
                        filePath);
                }
                catch (DirectoryNotFoundException)
                {
                    // on PS4 we're seeing CreateDirectory work but ReadAllText throw DirectoryNotFoundException
                    _options.DiagnosticLogger?.LogDebug(
                        "Directory containing installation ID does not exist ({0}).",
                        filePath);
                }

                // Generate new installation ID and store it in a file
                var id = Guid.NewGuid().ToString();
                File.WriteAllText(filePath, id);

                _options.DiagnosticLogger?.LogDebug(
                    "Saved installation ID '{0}' to file '{1}'.",
                    id, filePath);

                return id;
            }
            // If there's no write permission or the platform doesn't support this, we handle
            // and let the next installation id strategy kick in
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError(
                    "Failed to resolve persistent installation ID.",
                    ex);

                return null;
            }
        }

        private string? TryGetHardwareInstallationId()
        {
            try
            {
                // Get MAC address of the first network adapter
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
                        "Failed to find an appropriate network interface for installation ID.");

                    return null;
                }

                return installationId;
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError(
                    "Failed to resolve hardware installation ID.",
                    ex);

                return null;
            }
        }

        // Internal for testing
        internal string GetMachineNameInstallationId() =>
            // Never fails
            Environment.MachineName.GetHashString();

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
                    TryGetHardwareInstallationId() ??
                    GetMachineNameInstallationId();

                if (!string.IsNullOrWhiteSpace(id))
                {
                    _options.DiagnosticLogger?.LogDebug(
                        "Resolved installation ID '{0}'.",
                        id);
                }
                else
                {
                    _options.DiagnosticLogger?.LogDebug(
                        "Failed to resolve installation ID.");
                }

                return _resolvedInstallationId = id;
            }
        }

        // Take pause timestamp directly instead of referencing _lastPauseTimestamp to avoid
        // potential race conditions.
        private void PersistSession(SessionUpdate update, DateTimeOffset? pauseTimestamp = null)
        {
            _options.DiagnosticLogger?.LogDebug("Persisting session (SID: '{0}') to a file.", update.Id);

            if (string.IsNullOrWhiteSpace(_persistenceDirectoryPath))
            {
                _options.DiagnosticLogger?.LogDebug("Persistence directory is not set, returning.");
                return;
            }

            try
            {
                Directory.CreateDirectory(_persistenceDirectoryPath);

                _options.DiagnosticLogger?.LogDebug(
                    "Created persistence directory for session file '{0}'.",
                    _persistenceDirectoryPath);

                var filePath = Path.Combine(_persistenceDirectoryPath, PersistedSessionFileName);

                var persistedSessionUpdate = new PersistedSessionUpdate(update, pauseTimestamp);
                persistedSessionUpdate.WriteToFile(filePath);

                _options.DiagnosticLogger?.LogDebug("Persisted session to a file '{0}'.", filePath);
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError("Failed to persist session on the file system.", ex);
            }
        }

        private void DeletePersistedSession()
        {
            _options.DiagnosticLogger?.LogDebug("Deleting persisted session file.");

            if (string.IsNullOrWhiteSpace(_persistenceDirectoryPath))
            {
                _options.DiagnosticLogger?.LogDebug("Persistence directory is not set, returning.");
                return;
            }

            var filePath = Path.Combine(_persistenceDirectoryPath, PersistedSessionFileName);
            try
            {
                // Try to log the contents of the session file before we delete it
                if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Debug) ?? false)
                {
                    try
                    {
                        _options.DiagnosticLogger?.LogDebug(
                            "Deleting persisted session file with contents: {0}",
                            File.ReadAllText(filePath));
                    }
                    catch (Exception ex)
                    {
                        _options.DiagnosticLogger?.LogError(
                            "Failed to read the contents of persisted session file '{0}'.",
                            ex,
                            filePath);
                    }
                }

                File.Delete(filePath);

                _options.DiagnosticLogger?.LogInfo(
                    "Deleted persisted session file '{0}'.",
                    filePath);
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError(
                    "Failed to delete persisted session from the file system: '{0}'",
                    ex,
                    filePath);
            }
        }

        // internal for testing.
        internal SessionUpdate? TryRecoverPersistedSession()
        {
            _options.DiagnosticLogger?.LogDebug("Attempting to recover persisted session from file.");

            if (string.IsNullOrWhiteSpace(_persistenceDirectoryPath))
            {
                _options.DiagnosticLogger?.LogDebug("Persistence directory is not set, returning.");
                return null;
            }

            var filePath = Path.Combine(_persistenceDirectoryPath, PersistedSessionFileName);
            try
            {
                var recoveredUpdate = _persistedSessionProvider(filePath);

                // Create a session update to end the recovered session
                return new SessionUpdate(
                    recoveredUpdate.Update,
                    // We're recovering an ongoing session, so this can never be initial
                    false,
                    // If the session was paused, then use that as timestamp, otherwise use current timestamp
                    recoveredUpdate.PauseTimestamp ?? _clock.GetUtcNow(),
                    // Increment sequence number
                    recoveredUpdate.Update.SequenceNumber + 1,
                    // If the session was paused then end normally, otherwise abnormal or crashed
                    _options.CrashedLastRun switch
                    {
                        _ when recoveredUpdate.PauseTimestamp is not null => SessionEndStatus.Exited,
                        { } crashedLastRun => crashedLastRun() ? SessionEndStatus.Crashed : SessionEndStatus.Abnormal,
                        _ => SessionEndStatus.Abnormal
                    });
            }
            catch (IOException ioEx) when (ioEx is FileNotFoundException or DirectoryNotFoundException)
            {
                // Not a notable error
                _options.DiagnosticLogger?.LogDebug(
                    "A persisted session does not exist at {0}.",
                    filePath);

                return null;
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError(
                    "Failed to recover persisted session from the file system '{0}'.",
                    ex,
                    filePath);

                return null;
            }
        }

        public void StartSession()
        {
            // Attempt to recover persisted session left over from previous run
            if (Interlocked.Exchange(ref _isPersistedSessionRecovered, 1) != 1)
            {
                try
                {
                    var recoveredSessionUpdate = TryRecoverPersistedSession();
                    if (recoveredSessionUpdate is not null)
                    {
                        _client.CaptureSession(recoveredSessionUpdate);
                    }
                }
                catch (Exception ex)
                {
                    _options.DiagnosticLogger?.LogError(
                        "Failed to recover persisted session.",
                        ex
                    );
                }
            }

            // Extract release
            var release = ReleaseLocator.Resolve(_options);
            if (string.IsNullOrWhiteSpace(release))
            {
                // Release health without release is just health (useless)
                _options.DiagnosticLogger?.LogError(
                    "Failed to start a session because there is no release information.");

                return;
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
                    "Starting a new session while an existing one is still active.");

                // End previous session
                EndSession(previousSession, _clock.GetUtcNow(), SessionEndStatus.Exited);
            }

            _options.DiagnosticLogger?.LogInfo(
                "Started new session (SID: {0}; DID: {1}).",
                session.Id, session.DistinctId);

            var sessionUpdate = session.CreateUpdate(true, _clock.GetUtcNow());
            PersistSession(sessionUpdate);

            _client.CaptureSession(sessionUpdate);
            _scopeManager.ConfigureScope(scope => scope.SessionUpdate = sessionUpdate);
        }

        private void EndSession(Session session, DateTimeOffset timestamp, SessionEndStatus status)
        {
            _options.DiagnosticLogger?.LogInfo(
                "Ended session (SID: {0}; DID: {1}) with status '{2}'.",
                session.Id, session.DistinctId, status);

            var sessionUpdate = session.CreateUpdate(false, timestamp, status);
            DeletePersistedSession();

            _client.CaptureSession(sessionUpdate);
            _scopeManager.ConfigureScope(scope => scope.SessionUpdate = null);
        }

        public void EndSession(DateTimeOffset timestamp, SessionEndStatus status)
        {
            var session = Interlocked.Exchange(ref _currentSession, null);
            if (session is null)
            {
                _options.DiagnosticLogger?.LogDebug(
                    "Failed to end session because there is none active.");

                return;
            }

            EndSession(session, timestamp, status);
        }

        public void EndSession(SessionEndStatus status) => EndSession(_clock.GetUtcNow(), status);

        public void PauseSession()
        {
            lock (_pauseResumeLock)
            {
                if (_currentSession is { } session)
                {
                    var now = _clock.GetUtcNow();
                    _lastPauseTimestamp = now;
                    PersistSession(session.CreateUpdate(false, now), now);
                }
            }
        }

        public void ResumeSession()
        {
            lock (_pauseResumeLock)
            {
                // Ensure a session has been paused before
                if (_lastPauseTimestamp is not { } sessionPauseTimestamp)
                {
                    _options.DiagnosticLogger?.LogDebug(
                        "Attempted to resume a session, but the current session hasn't been paused.");

                    return;
                }

                // Reset the pause timestamp since the session is about to be resumed
                _lastPauseTimestamp = null;

                // If the pause duration exceeded tracking interval, start a new session
                // (otherwise do nothing)
                var pauseDuration = (_clock.GetUtcNow() - sessionPauseTimestamp).Duration();
                if (pauseDuration >= _options.AutoSessionTrackingInterval)
                {
                    _options.DiagnosticLogger?.LogDebug(
                        "Paused session has been paused for {0}, which is longer than the configured timeout. " +
                        "Starting a new session instead of resuming this one.",
                        pauseDuration);

                    EndSession(sessionPauseTimestamp, SessionEndStatus.Exited);
                    StartSession();
                }

                _options.DiagnosticLogger?.LogDebug(
                    "Paused session has been paused for {0}, which is shorter than the configured timeout.",
                    pauseDuration);
            }
        }

        public void ReportError()
        {
            if (_currentSession is { } session)
            {
                session.ReportError();

                var sessionUpdate = session.CreateUpdate(false, _clock.GetUtcNow());
                _scopeManager.ConfigureScope(scope => scope.SessionUpdate = sessionUpdate);
            }

            _options.DiagnosticLogger?.LogDebug(
                "Failed to report an error on a session because there is none active.");
        }

        public void Dispose()
        {
            // No-op. TODO: perhaps end session?
        }
    }
}
