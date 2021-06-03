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
        private readonly SentryOptions _options;

        private Session? _currentSession;
        public Session? CurrentSession => _currentSession;

        public GlobalSessionManager(SentryOptions options)
        {
            _options = options;
        }

        private string GetInstallationId()
        {
            var filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Sentry",
                ".installation"
            );

            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Try to read existing
            try
            {
                return File.ReadAllText(filePath);
            }
            catch (FileNotFoundException)
            {
            }

            // Generate new
            var id = Guid.NewGuid().ToString();
            File.WriteAllText(filePath, id);

            return id;
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
