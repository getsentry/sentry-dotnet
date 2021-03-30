using System.Collections.Generic;

namespace Sentry.AspNetCore
{
    public static class SamplingExtensions
    {
        internal const string KeyForHttpRoute = "__HttpRoute";
        internal const string KeyForHttpPath = "__HttpPath";

        /// <summary>
        /// Gets the HTTP route associated with the transaction.
        /// </summary>
        /// <remarks>
        /// This method extracts data from <see cref="TransactionSamplingContext.CustomSamplingContext"/>
        /// which is populated by Sentry's ASP.NET Core integration.
        /// </remarks>
        public static string? TryGetHttpRoute(this TransactionSamplingContext samplingContext) =>
            samplingContext.CustomSamplingContext.GetValueOrDefault(KeyForHttpRoute) as string;

        /// <summary>
        /// Gets the HTTP path associated with the transaction.
        /// </summary>
        /// <remarks>
        /// This method extracts data from <see cref="TransactionSamplingContext.CustomSamplingContext"/>
        /// which is populated by Sentry's ASP.NET Core integration.
        /// </remarks>
        public static string? TryGetHttpPath(this TransactionSamplingContext samplingContext) =>
            samplingContext.CustomSamplingContext.GetValueOrDefault(KeyForHttpPath) as string;
    }
}
