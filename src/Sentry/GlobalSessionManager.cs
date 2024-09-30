using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry;

// AKA client mode
internal class GlobalSessionManager : ISessionManager
{
    private const string PersistedSessionFileName = ".session";

    private readonly ISystemClock _clock;
    private readonly Func<string, PersistedSessionUpdate> _persistedSessionProvider;
    private readonly SentryOptions _options;

    private readonly string? _persistenceDirectoryPath;

    private SentrySession? _currentSession;
    private DateTimeOffset? _lastPauseTimestamp;

    // Internal for testing
    internal SentrySession? CurrentSession => _currentSession;

    public bool IsSessionActive => _currentSession is not null;

    public GlobalSessionManager(
        SentryOptions options,
        ISystemClock? clock = null,
        Func<string, PersistedSessionUpdate>? persistedSessionProvider = null)
    {
        _options = options;
        _clock = clock ?? SystemClock.Clock;
        _persistedSessionProvider = persistedSessionProvider
                                    ?? (filePath => Json.Load(_options.FileSystem, filePath, PersistedSessionUpdate.FromJson));

        // TODO: session file should really be process-isolated, but we
        // don't have a proper mechanism for that right now.
        _persistenceDirectoryPath = options.TryGetDsnSpecificCacheDirectoryPath();
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

        if (_options.DisableFileWrite)
        {
            _options.LogInfo("File write has been disabled via the options. Skipping persisting session.");
            return;
        }

        try
        {
            _options.LogDebug("Creating persistence directory for session file at '{0}'.", _persistenceDirectoryPath);

            if (_options.FileSystem.CreateDirectory(_persistenceDirectoryPath) is not true)
            {
                _options.LogError("Failed to create persistent directory for session file.");
                return;
            }

            var filePath = Path.Combine(_persistenceDirectoryPath, PersistedSessionFileName);

            var persistedSessionUpdate = new PersistedSessionUpdate(update, pauseTimestamp);
            if (_options.FileSystem.CreateFileForWriting(filePath, out var file) is not true)
            {
                _options.LogError("Failed to persist session file.");
                return;
            }

            using var writer = new Utf8JsonWriter(file);

            try
            {
                persistedSessionUpdate.WriteTo(writer, _options.DiagnosticLogger);
                writer.Flush();
            }
            finally
            {
                file.Dispose();
            }

            _options.LogDebug("Persisted session to a file '{0}'.", filePath);
        }
        catch (Exception ex)
        {
            _options.LogError(ex, "Failed to persist session on the file system.");
        }
    }

    private void DeletePersistedSession()
    {
        if (string.IsNullOrWhiteSpace(_persistenceDirectoryPath))
        {
            _options.LogDebug("Persistence directory is not set, not deleting any persisted session file.");
            return;
        }

        if (_options.DisableFileWrite)
        {
            _options.LogInfo("File write has been disabled via the options. Skipping deleting persisted session.");
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
                    var contents = _options.FileSystem.ReadAllTextFromFile(filePath);
                    _options.LogDebug("Deleting persisted session file with contents: {0}", contents);
                }
                catch (Exception ex)
                {
                    _options.LogError(ex, "Failed to read the contents of persisted session file '{0}'.", filePath);
                }
            }

            if (_options.FileSystem.DeleteFile(filePath) is not true)
            {
                _options.LogError("Failed to delete persisted session file.");
                return;
            }

            _options.LogInfo("Deleted persisted session file '{0}'.", filePath);
        }
        catch (Exception ex)
        {
            _options.LogError(ex, "Failed to delete persisted session from the file system: '{0}'", filePath);
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
                _options.LogError(e, "Invoking CrashedLastRun failed.");
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
            _options.LogError(ex, "Failed to recover persisted session from the file system '{0}'.", filePath);

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
            _options.LogError("Failed to start a session because there is no release information.");

            return null;
        }

        // Extract other parameters
        var environment = _options.SettingLocator.GetEnvironment();
        var distinctId = _options.InstallationId;

        // Create new session
        var session = new SentrySession(distinctId, release, environment);

        // Set new session and check whether we ended up overwriting an active one in the process
        var previousSession = Interlocked.Exchange(ref _currentSession, session);
        if (previousSession is not null)
        {
            _options.LogWarning("Starting a new session while an existing one is still active.");

            // End previous session
            EndSession(previousSession, _clock.GetUtcNow(), SessionEndStatus.Exited);
        }

        _options.LogInfo("Started new session (SID: {0}; DID: {1}).", session.Id, session.DistinctId);

        var update = session.CreateUpdate(true, _clock.GetUtcNow());

        PersistSession(update);

        return update;
    }

    private SessionUpdate EndSession(SentrySession session, DateTimeOffset timestamp, SessionEndStatus status)
    {
        if (status == SessionEndStatus.Crashed)
        {
            // increments the errors count, as crashed sessions should report a count of 1 per:
            // https://develop.sentry.dev/sdk/sessions/#session-update-payload
            session.ReportError();
        }

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
            _options.LogWarning("Failed to end session because there is none active.");
            return null;
        }

        return EndSession(session, timestamp, status);
    }

    public SessionUpdate? EndSession(SessionEndStatus status) => EndSession(_clock.GetUtcNow(), status);

    public void PauseSession()
    {
        if (_currentSession is not { } session)
        {
            _options.LogWarning("Attempted to pause a session, but a session has not been started.");
            return;
        }

        _options.LogInfo("Pausing session (SID: {0}; DID: {1}).", session.Id, session.DistinctId);

        var now = _clock.GetUtcNow();
        _lastPauseTimestamp = now;
        PersistSession(session.CreateUpdate(false, now), now);
    }

    public IReadOnlyList<SessionUpdate> ResumeSession()
    {
        if (_currentSession is not { } session)
        {
            _options.LogWarning("Attempted to resume a session, but a session has not been started.");
            return Array.Empty<SessionUpdate>();
        }

        // Ensure a session has been paused before
        if (_lastPauseTimestamp is not { } sessionPauseTimestamp)
        {
            _options.LogWarning("Attempted to resume a session, but the current session hasn't been paused.");
            return Array.Empty<SessionUpdate>();
        }

        _options.LogInfo("Resuming session (SID: {0}; DID: {1}).", session.Id, session.DistinctId);

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

        _options.LogInfo("Resumed session (SID: {0}; DID: {1}) after being paused for {2}.",
            session.Id, session.DistinctId, pauseDuration);

        return Array.Empty<SessionUpdate>();
    }

    public SessionUpdate? ReportError()
    {
        if (_currentSession is not { } session)
        {
            _options.LogDebug("There is no session active. Skipping updating the session as errored. " +
                              "Consider setting 'AutoSessionTracking = true' to enable Release Health and crash free rate.");
            return null;
        }

        session.ReportError();

        // If we already have at least one error reported, the session update is pointless, so don't return anything.
        if (session.ErrorCount > 1)
        {
            _options.LogDebug("Reported an error on a session that already contains errors. Not creating an update.");
            return null;
        }

        return session.CreateUpdate(false, _clock.GetUtcNow());
    }
}
