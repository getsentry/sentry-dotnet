using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging
{
    internal static class EventIdExtensions
    {
        public const string DataKey = "eventId";

        /// <summary>
        /// Returns a tuple (eventId,value) if the event is not empty, otherwise null.
        /// </summary>
        public static (string name, string value)? ToTupleOrNull(this EventId eventId)
        {
            (string, string)? data = null;

            if (eventId.Id != 0 || eventId.Name != null)
            {
                data = (DataKey, eventId.ToString());
            }

            return data;
        }

        /// <summary>
        /// Returns a dictionary (eventId,value) if the event is not empty, otherwise null.
        /// </summary>
        public static IDictionary<string, string>? ToDictionaryOrNull(this EventId eventId)
        {
            return eventId.Id != 0 || eventId.Name != null
                ? new Dictionary<string, string>
                {
                    {DataKey, eventId.ToString()}
                }
                : null;
        }
    }
}
