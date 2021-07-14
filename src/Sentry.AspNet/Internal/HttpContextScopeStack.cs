using System.Collections.Generic;
using System.Web;
using Sentry.Internal.ScopeStack;

namespace Sentry.AspNet.Internal
{
    internal class HttpContextScopeStack : IScopeStackContainer
    {
        private const string FieldName = "__SentryScopeStack";
        private readonly HttpContext _httpContext;

        public KeyValuePair<Scope, ISentryClient>[]? Stack
        {
            get => _httpContext.Items[FieldName] as KeyValuePair<Scope, ISentryClient>[];
            set => _httpContext.Items[FieldName] = value;
        }

        public HttpContextScopeStack(HttpContext httpContext) =>
            _httpContext = httpContext;

        public HttpContextScopeStack()
            : this(HttpContext.Current)
        {
        }
    }
}
