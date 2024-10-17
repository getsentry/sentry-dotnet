using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentry
{
    /// <summary>
    /// Handler strategy
    /// </summary>
    /// 
    public enum NdkHandlerStrategy
    {
        /// <summary>
        /// default handler strategy -> value 0
        /// </summary>
        SENTRY_HANDLER_STRATEGY_DEFAULT,
        /// <summary>
        /// Handle strategy chain at start -> value 1
        /// </summary>
        SENTRY_HANDLER_STRATEGY_CHAIN_AT_START,

    }
}
