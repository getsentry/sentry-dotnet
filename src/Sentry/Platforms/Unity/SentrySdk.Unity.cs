#if SENTRY_UNITY

namespace Sentry;

/// <summary>
/// Internal Sentry SDK entrypoint.
/// </summary>
/// <remarks>
/// This class is now internal. Use <c>Sentry.Unity.SentrySdk</c> instead.
/// <para>
/// To migrate your code:
/// <list type="number">
/// <item>Change <c>using Sentry;</c> to <c>using Sentry.Unity;</c></item>
/// <item>Keep using the <c>SentrySdk</c> API itself - no changes needed to method calls</item>
/// <item>Add <c>using Sentry;</c> if you need access to types like <c>SentryId</c>, <c>SentryLevel</c>, etc.</item>
/// </list>
/// </para>
/// </remarks>
internal static partial class SentrySdk;

#endif
