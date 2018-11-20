using System;
using System.Collections.Generic;
using System.Text;

namespace Sentry
{
    public readonly struct SentryId
    {
        private readonly Guid _eventId;

        public static readonly Guid Empty;

        public SentryId(Guid guid) => _eventId = guid;

        public override string ToString() => _eventId.ToString("n");

        public static implicit operator Guid(SentryId sentryId) => sentryId._eventId;

        public static implicit operator SentryId(Guid guid) => new SentryId(guid);
    }
}
