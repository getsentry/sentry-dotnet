using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;

namespace Sentry.Protocol
{
    ///
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ScopeExtensions
    {
        ///
        public static void AddBreadcrumb(
            this Scope scope,
            string message,
            string type,
            string category = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
        {
            scope.AddBreadcrumb(new Breadcrumb(
                message: message,
                type: "logger",
                data: data?.ToImmutableDictionary(),
                category: category,
                level: level));
        }

        ///
        public static void AddBreadcrumb(
            this Scope scope,
            string message,
            string type,
            string category = null,
            (string, string)? dataPair = null,
            BreadcrumbLevel level = default)
        {
            var data = ImmutableDictionary<string, string>.Empty;
            if (dataPair.HasValue)
            {
                data = data.Add(dataPair.Value.Item1, dataPair.Value.Item2);
            }

            scope.AddBreadcrumb(
                message: message,
                type: "logger",
                data: data,
                category: category,
                level: level);
        }
    }
}
