using System.Linq;
using System.Net.NetworkInformation;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry
{
    // AKA client mode
    internal class GlobalSessionManager : ISessionManager
    {
        private readonly SentryOptions _options;

        public Session? CurrentSession { get; private set; }

        public GlobalSessionManager(SentryOptions options)
        {
            _options = options;
        }

        public Session? StartSession()
        {
            if (CurrentSession is not null)
            {
                _options.DiagnosticLogger?.LogWarning(
                    "Starting a new session while an existing one is still active."
                );

                // End previous session (TODO: should this be abnormal instead?)
                EndSession(SessionEndState.Exited);
            }

            var release = ReleaseLocator.Resolve(_options);
            if (string.IsNullOrWhiteSpace(release))
            {
                // Release health without release is just health (useless)
                _options.DiagnosticLogger?.LogError(
                    "Failed to start a session because there is no release information."
                );

                return null;
            }

            var environment = EnvironmentLocator.Resolve(_options);

            // TODO: proper distinct id
            var distinctId = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic =>
                    nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault();

            var session = new Session(distinctId, release, environment);
            CurrentSession = session;

            _options.DiagnosticLogger?.LogInfo(
                "Started new session (SID: {0}; DID: {1}).",
                session.Id, session.DistinctId
            );

            return session;
        }

        public Session? EndSession(SessionEndState state)
        {
            var session = CurrentSession;
            if (session is null)
            {
                _options.DiagnosticLogger?.LogError(
                    "Failed to end session because there is none active."
                );

                return null;
            }

            session.End(state);

            _options.DiagnosticLogger?.LogInfo(
                "Ended session (SID: {0}; DID: {1}) with state '{2}'.",
                session.Id, session.DistinctId, state
            );

            CurrentSession = null;

            return session;
        }
    }
}
