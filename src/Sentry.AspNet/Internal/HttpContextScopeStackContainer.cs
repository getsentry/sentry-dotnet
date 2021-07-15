using System.Collections.Generic;
using System.Web;
using Sentry.Internal.ScopeStack;

namespace Sentry.AspNet.Internal
{
    internal class HttpContextScopeStackContainer : IScopeStackContainer
    {
        private const string FieldName = "__SentryScopeStack";

        public KeyValuePair<Scope, ISentryClient>[]? Stack
        {
            get => HttpContext.Current.Items[FieldName] as KeyValuePair<Scope, ISentryClient>[];
            set => HttpContext.Current.Items[FieldName] = value;
        }
    }
}
