using System.Collections.Generic;
using System.Linq;

namespace Sentry.Samples.EntityFramework
{
    public static class IQueryableExtensions
    {
        public static List<T> ToList2<T>(this IQueryable<T> query) => query.ToList();
    }
}
