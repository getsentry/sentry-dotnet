using System.Collections;
using System.Collections.Generic;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace Sentry.NLog
{
    /// <summary>
    /// A helper class used to configure Sentry user properties using NLog layouts
    /// </summary>
    [NLogConfigurationItem]
    public class SentryNLogUser
    {
        /// <summary>
        /// A <see cref="Layout"/> used to dynamically specify the id of a user for a sentry event.
        /// </summary>
        public Layout Id { get; set; }

        /// <summary>
        /// A <see cref="Layout"/> used to dynamically specify the username of a user for a sentry event.
        /// </summary>
        public Layout Username { get; set; }

        /// <summary>
        /// A <see cref="Layout"/> used to dynamically specify the email of a user for a sentry event.
        /// </summary>
        public Layout Email { get; set; }

        /// <summary>
        /// A <see cref="Layout"/> used to dynamically specify the ip address of a user for a sentry event.
        /// </summary>
        public Layout IpAddress { get; set; }

        /// <summary>
        /// Additional information about the user
        /// </summary>
        /// <summary>
        /// Add any desired additional tags that will be sent with every message.
        /// </summary>
        [ArrayParameter(typeof(TargetPropertyWithContext), "other")]
        public IList<TargetPropertyWithContext> Other { get; } = new List<TargetPropertyWithContext>();
    }
}
