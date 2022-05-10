using System.Collections.Generic;

namespace Sentry.Internal.ScopeStack
{
    internal class GlobalScopeStackContainer : IScopeStackContainer
    {
        public KeyValuePair<Scope, ISentryClient>[]? Stack { get; set; }
    }
}
