using System;
using System.Collections.Generic;
using System.Text;

namespace Sentry.Extensibility
{
    interface IHub : ISentryClient, ISentryScopeManagement
    {
    }
}
