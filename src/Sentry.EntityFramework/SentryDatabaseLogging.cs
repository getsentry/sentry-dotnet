using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentry.EntityFramework
{
    public static class SentryDatabaseLogging
    {
        /// <summary>
        /// Adds an instance of <see cref="SentryCommandInterceptor"/> to <see cref="DbInterception"/>
        /// This is a static setup call, so make sure you only call it once for each <see cref="IQueryLogger"/> instance you want to register globally
        /// </summary>
        /// <param name="logger"></param>
        public static SentryCommandInterceptor UseBreadcrumbs(IQueryLogger logger = null)
        {
            logger = logger ?? new SentryQueryLogger();
            var interceptor = new SentryCommandInterceptor(logger);
            DbInterception.Add(interceptor);
            return interceptor;
        }
    }
}
