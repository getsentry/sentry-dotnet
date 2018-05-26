using System;

namespace Sentry
{
    /// TODO: the SDK options
    public class SentryOptions
    {
        /// 
        public bool CompressPayload { get; set; } = true;
        /// 
        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(3);
    }
}
