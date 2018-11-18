using System;
using System.Collections.Generic;
using System.Text;

namespace Sentry
{
   public struct SentryId
    {
       public Guid eventID;

        public override string ToString()
        {
          return eventID.ToString("n");
        }

        public static implicit operator Guid(SentryId d)
        {
            return d.eventID;
        }
    }
}
