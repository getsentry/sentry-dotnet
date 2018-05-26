using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class LogLevelExtensionsTests
    {
        [Theory]
        [MemberData(nameof(TestCases))]
        public void ToBreadcrumbLevel_TestCases((BreadcrumbLevel expected, LogLevel sut) @case)
            => Assert.Equal(@case.expected, @case.sut.ToBreadcrumbLevel());

        public static IEnumerable<object[]> TestCases()
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
    }
}
