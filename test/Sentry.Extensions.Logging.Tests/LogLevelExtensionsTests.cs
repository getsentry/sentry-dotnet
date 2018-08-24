using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class LogLevelExtensionsTests
    {
        [Theory]
        [MemberData(nameof(BreadcrumbTestCases))]
        public void ToBreadcrumbLevel_TestCases((BreadcrumbLevel expected, LogLevel sut) @case)
            => Assert.Equal(@case.expected, @case.sut.ToBreadcrumbLevel());

        public static IEnumerable<object[]> BreadcrumbTestCases()
        {
            yield return new object[] { (BreadcrumbLevel.Debug, LogLevel.Trace) };
            yield return new object[] { (BreadcrumbLevel.Debug, LogLevel.Debug) };
            yield return new object[] { (BreadcrumbLevel.Info, LogLevel.Information) };
            yield return new object[] { (BreadcrumbLevel.Warning, LogLevel.Warning) };
            yield return new object[] { (BreadcrumbLevel.Error, LogLevel.Error) };
            yield return new object[] { (BreadcrumbLevel.Critical, LogLevel.Critical) };
            yield return new object[] { ((BreadcrumbLevel)6, LogLevel.None) };
            yield return new object[] { ((BreadcrumbLevel)int.MaxValue, (LogLevel)int.MaxValue) };
        }

        [Theory]
        [MemberData(nameof(MelTestCases))]
        public void ToMicrosoft_TestCases((LogLevel expected, SentryLevel sut) @case)
            => Assert.Equal(@case.expected, @case.sut.ToMicrosoft());

        public static IEnumerable<object[]> MelTestCases()
        {
            yield return new object[] { (LogLevel.Debug, SentryLevel.Debug) };
            yield return new object[] { (LogLevel.Information, SentryLevel.Info) };
            yield return new object[] { (LogLevel.Warning, SentryLevel.Warning) };
            yield return new object[] { (LogLevel.Error, SentryLevel.Error) };
            yield return new object[] { (LogLevel.Critical, SentryLevel.Fatal) };
        }

        [Theory]
        [MemberData(nameof(LogLevelToSentryLevel))]
        public void ToSentryLevel_TestCases((SentryLevel expected, LogLevel sut) @case)
            => Assert.Equal(@case.expected, @case.sut.ToSentryLevel());

        public static IEnumerable<object[]> LogLevelToSentryLevel()
        {
            yield return new object[] { (SentryLevel.Debug, LogLevel.Trace) };
            yield return new object[] { (SentryLevel.Debug, LogLevel.Debug) };
            yield return new object[] { (SentryLevel.Info, LogLevel.Information) };
            yield return new object[] { (SentryLevel.Warning, LogLevel.Warning) };
            yield return new object[] { (SentryLevel.Error, LogLevel.Error) };
            yield return new object[] { (SentryLevel.Fatal, LogLevel.Critical) };
        }
    }
}
