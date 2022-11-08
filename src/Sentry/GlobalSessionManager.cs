using System.Net.NetworkInformation;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry;

// AKA client mode
internal class GlobalSessionManager : ISessionManager
{
    private const string PersistedSessionFileName = ".session";

    private readonly object _installationIdLock = new();

    private readonly ISystemClock _clock;
    private readonly Func<string, PersistedSessionUpdate> _persistedSessionProvider;
    private readonly SentryOptions _options;

    private readonly string? _persistenceDirectoryPath;

    private string? _resolvedInstallationId;
    private Session? _currentSession;
    private DateTimeOffset? _lastPauseTimestamp;

    // Internal for testing
    internal Session? CurrentSession => _currentSession;

    public bool IsSessionActive => _currentSession is not null;

    public GlobalSessionManager(
        SentryOptions options,
        ISystemClock? clock = null,
        Func<string, PersistedSessionUpdate>? persistedSessionProvider = null)
    {
        _options = options;
        _clock = clock ?? SystemClock.Clock;
        _persistedSessionProvider = persistedSessionProvider
                                    ?? (filePath => Json.Load(filePath, PersistedSessionUpdate.FromJson));

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

            _options.LogDebug("Created directory for installation ID file ({0}).",
                directoryPath);

            var filePath = Path.Combine(directoryPath, ".installation");

            // Read installation ID stored in a file
            try
            {
                return File.ReadAllText(filePath);
            }
            catch (FileNotFoundException)
            {
                _options.LogDebug("File containing installation ID does not exist ({0}).",
                    filePath);
            }
            catch (DirectoryNotFoundException)
            {
                // on PS4 we're seeing CreateDirectory work but ReadAllText throw DirectoryNotFoundException
                _options.LogDebug(
                    "Directory containing installation ID does not exist ({0}).",
                    filePath);
            }

            // Generate new installation ID and store it in a file
            var id = Guid.NewGuid().ToString();
            File.WriteAllText(filePath, id);

            _options.LogDebug("Saved installation ID '{0}' to file '{1}'.",
                id, filePath);

            return id;
        }
        // If there's no write permission or the platform doesn't support this, we handle
        // and let the next installation id strategy kick in
        catch (Exception ex)
        {
            _options.LogError("Failed to resolve persistent installation ID.", ex);

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
                _options.LogError(
                    "Failed to find an appropriate network interface for installation ID.");

                return null;
            }

            return installationId;
        }
        catch (Exception ex)
        {
            _options.LogError("Failed to resolve hardware installation ID.", ex);

            return null;
        }
    }

    // Internal for testing
    internal static string GetMachineNameInstallationId() =>
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
                GlobalSessionManager.GetMachineNameInstallationId();

            if (!string.IsNullOrWhiteSpace(id))
            {
                _options.LogDebug("Resolved installation ID '{0}'.", id);
            }
            else
            {
                _options.LogDebug("Failed to resolve installation ID.");
            }

            return _resolvedInstallationId = id;
        }
    }

    // Take pause timestamp directly instead of referencing _lastPauseTimestamp to avoid
    // potential race conditions.
    private void PersistSession(SessionUpdate update, DateTimeOffset? pauseTimestamp = null)
    {
        _options.LogDebug("Persisting session (SID: '{0}') to a file.", update.Id);

        if (string.IsNullOrWhiteSpace(_persistenceDirectoryPath))
        {
            _options.LogDebug("Persistence directory is not set, returning.");
            return;
        }

        try
        {
            Directory.CreateDirectory(_persistenceDirectoryPath);

            _options.LogDebug(
                "Created persistence directory for session file '{0}'.",
                _persistenceDirectoryPath);

            var filePath = Path.Combine(_persistenceDirectoryPath, PersistedSessionFileName);

            var persistedSessionUpdate = new PersistedSessionUpdate(update, pauseTimestamp);
            persistedSessionUpdate.WriteToFile(filePath, _options.DiagnosticLogger);

            _options.LogDebug("Persisted session to a file '{0}'.", filePath);
        }
        catch (Exception ex)
        {
            _options.LogError("Failed to persist session on the file system.", ex);
        }
    }

    private void DeletePersistedSession()
    {
        if (string.IsNullOrWhiteSpace(_persistenceDirectoryPath))
        {
            _options.LogDebug("Persistence directory is not set, not deleting any persisted session file.");
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
                    _options.LogDebug("Deleting persisted session file with contents: {0}",
                        File.ReadAllText(filePath));
                }
                catch (Exception ex)
                {
                    _options.LogError(
                        "Failed to read the contents of persisted session file '{0}'.",
                        ex,
                        filePath);
                }
            }

            File.Delete(filePath);

            _options.LogInfo("Deleted persisted session file '{0}'.", filePath);
        }
        catch (Exception ex)
        {
            _options.LogError(
                "Failed to delete persisted session from the file system: '{0}'",
                ex,
                filePath);
        }
    }

    public SessionUpdate? TryRecoverPersistedSession()
    {
        _options.LogDebug("Attempting to recover persisted session from file.");

        if (string.IsNullOrWhiteSpace(_persistenceDirectoryPath))
        {
            _options.LogDebug("Persistence directory is not set, returning.");
            return null;
        }

        var filePath = Path.Combine(_persistenceDirectoryPath, PersistedSessionFileName);
        try
        {
            var recoveredUpdate = _persistedSessionProvider(filePath);

            SessionEndStatus? status = null;
            try
            {
                status = _options.CrashedLastRun?.Invoke() switch
                {
                    // Native crash (if native SDK enabled):
                    true => SessionEndStatus.Crashed,
                    // Ended while on the background, healthy session:
                    _ when recoveredUpdate.PauseTimestamp is not null => SessionEndStatus.Exited,
                    // Possibly out of battery, killed by OS or user, solar flare:
                    _ => SessionEndStatus.Abnormal
                };
            }
            catch (Exception e)
            {
                _options.LogError("Invoking CrashedLastRun failed.", e);
            }

            // Create a session update to end the recovered session
            var sessionUpdate = new SessionUpdate(
                recoveredUpdate.Update,
                // We're recovering an ongoing session, so this can never be initial
                false,
                // If the session was paused, then use that as timestamp, otherwise use current timestamp
                recoveredUpdate.PauseTimestamp ?? _clock.GetUtcNow(),
                // Increment sequence number
                recoveredUpdate.Update.SequenceNumber + 1,
                // If there's a callback for native crashes, check that first.
                status);

            _options.LogInfo("Recovered session: EndStatus: {0}. PauseTimestamp: {1}",
                sessionUpdate.EndStatus,
                recoveredUpdate.PauseTimestamp);

            return sessionUpdate;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            // Not a notable error
            _options.LogDebug("A persisted session does not exist ({0}) at {1}.", ex.GetType().Name, filePath);
            return null;
        }
        catch (Exception ex)
        {
            _options.LogError("Failed to recover persisted session from the file system '{0}'.", ex, filePath);

            return null;
        }
    }

    public SessionUpdate? StartSession()
    {
        // Extract release
        var release = _options.SettingLocator.GetRelease();
        if (string.IsNullOrWhiteSpace(release))
        {
            // Release health without release is just health (useless)
            _options.LogError(
                "Failed to start a session because there is no release information.");

            return null;
        }

        // Extract other parameters
        var environment = _options.SettingLocator.GetEnvironment();
        var distinctId = TryGetInstallationId();

        // Create new session
        var session = new Session(distinctId, release, environment);

        // Set new session and check whether we ended up overwriting an active one in the process
        var previousSession = Interlocked.Exchange(ref _currentSession, session);
        if (previousSession is not null)
        {
            _options.LogWarning(
                "Starting a new session while an existing one is still active.");

            // End previous session
            EndSession(previousSession, _clock.GetUtcNow(), SessionEndStatus.Exited);
        }

        AddSessionBreadcrumb("Starting Sentry Session");
        _options.LogInfo("Started new session (SID: {0}; DID: {1}).",
            session.Id, session.DistinctId);

        var update = session.CreateUpdate(true, _clock.GetUtcNow());

        PersistSession(update);

        return update;
    }

    private SessionUpdate EndSession(Session session, DateTimeOffset timestamp, SessionEndStatus status)
    {
        if (status == SessionEndStatus.Crashed)
        {
            // increments the errors count, as crashed sessions should report a count of 1 per:
            // https://develop.sentry.dev/sdk/sessions/#session-update-payload
            session.ReportError();
        }

        AddSessionBreadcrumb("Ending Sentry Session");
        _options.LogInfo("Ended session (SID: {0}; DID: {1}) with status '{2}'.",
            session.Id, session.DistinctId, status);

        var update = session.CreateUpdate(false, timestamp, status);

        DeletePersistedSession();

        return update;
    }

    public SessionUpdate? EndSession(DateTimeOffset timestamp, SessionEndStatus status)
    {
        var session = Interlocked.Exchange(ref _currentSession, null);
        if (session is null)
        {
            _options.LogDebug("Failed to end session because there is none active.");

            return null;
        }

        return EndSession(session, timestamp, status);
    }

    public SessionUpdate? EndSession(SessionEndStatus status) => EndSession(_clock.GetUtcNow(), status);

    public void PauseSession()
    {
        if (_currentSession is { } session)
        {
            AddSessionBreadcrumb("Pausing Sentry Session");

            var now = _clock.GetUtcNow();
            _lastPauseTimestamp = now;
            PersistSession(session.CreateUpdate(false, now), now);
        }
    }

    public IReadOnlyList<SessionUpdate> ResumeSession()
    {
        // Ensure a session has been paused before
        if (_lastPauseTimestamp is not { } sessionPauseTimestamp)
        {
            _options.LogDebug(
                "Attempted to resume a session, but the current session hasn't been paused.");

            return Array.Empty<SessionUpdate>();
        }

        AddSessionBreadcrumb("Resuming Sentry Session");

        // Reset the pause timestamp since the session is about to be resumed
        _lastPauseTimestamp = null;

        // If the pause duration exceeded tracking interval, start a new session
        // (otherwise do nothing)
        var pauseDuration = (_clock.GetUtcNow() - sessionPauseTimestamp).Duration();
        if (pauseDuration >= _options.AutoSessionTrackingInterval)
        {
            _options.LogDebug(
                "Paused session has been paused for {0}, which is longer than the configured timeout. " +
                "Starting a new session instead of resuming this one.",
                pauseDuration);

            var updates = new List<SessionUpdate>(2);

            // End current session
            if (EndSession(sessionPauseTimestamp, SessionEndStatus.Exited) is { } endUpdate)
            {
                updates.Add(endUpdate);
            }

            // Start a new session
            if (StartSession() is { } startUpdate)
            {
                updates.Add(startUpdate);
            }

            return updates;
        }

        _options.LogDebug(
            "Paused session has been paused for {0}, which is shorter than the configured timeout.",
            pauseDuration);

        return Array.Empty<SessionUpdate>();
    }

    public SessionUpdate? ReportError()
    {
        if (_currentSession is { } session)
        {
            session.ReportError();

            // If we already have at least one error reported, the session update is pointless,
            // so don't return anything.
            if (session.ErrorCount > 1)
            {
                _options.LogDebug(
                    "Reported an error on a session that already contains errors. Not creating an update.");

                return null;
            }

            return session.CreateUpdate(false, _clock.GetUtcNow());
        }

        _options.LogDebug(
            "Failed to report an error on a session because there is none active.");

        return null;
    }

    private static void AddSessionBreadcrumb(string message)
        => SentrySdk.AddBreadcrumb(message, "app.lifecycle", "session");
}
