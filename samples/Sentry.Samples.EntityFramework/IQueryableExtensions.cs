using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sentry.Samples.EntityFramework
{
    public static class IQueryableExtensions
    {
        public static List<T> ToList2<T>(IQueryable<T> query)
        {
            return query.ToList();
        }

    }
}
