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
    /// <summary>
    /// Extension class of strategy containing relevant its actions
    /// </summary>
    public static class  HandlerStrategyExtension
    {
        internal static JavaSdk.Android.Core.NdkHandlerStrategy ToJava(this NdkHandlerStrategy strategy)
        {
            return JavaSdk.Android.Core.NdkHandlerStrategy.Values()![(int)strategy];
        }
    }
}
