using Sentry.PlatformAbstractions;

namespace Sentry.Tests.PlatformAbstractions;

public class RuntimeInfoTests
{
    [Fact]
    public void GetRuntime_AllProperties()
    {
        var actual = RuntimeInfo.GetRuntime();
        Assert.NotNull(actual);
        Assert.NotNull(actual.Name);
        Assert.NotNull(actual.Version);
        Assert.NotNull(actual.Raw);

#if NET5_0_OR_GREATER
        Assert.Equal(".NET", actual.Name);
        Assert.NotNull(actual.Identifier);
#elif NETCOREAPP
        Assert.Equal(".NET Core", actual.Name);
#elif NETFRAMEWORK
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Equal(".NET Framework", actual.Name);
            Assert.NotNull(actual.FrameworkInstallation);
            Assert.NotNull(actual.FrameworkInstallation.Version);
        }
        else
        {
            Assert.Equal("Mono", actual.Name);
            Assert.Null(actual.FrameworkInstallation);
        }
#endif
    }

    [Fact]
    public void GetRuntime_NetCoreVersion()
    {
        var actual = RuntimeInfo.GetRuntime();
        Assert.NotNull(actual);

        #if NET8_0
        Assert.StartsWith("8.0", actual.Version);
        #elif NET7_0
        Assert.StartsWith("7.0", actual.Version);
        #elif NET6_0
        Assert.StartsWith("6.0", actual.Version);
        #elif NETCOREAPP3
        Assert.StartsWith("3", actual.Version);
        #endif
    }

    [Theory]
    [MemberData(nameof(ParseTestCases))]
    public void Parse_TestCases(ParseTestCase parseTestCase)
    {
        var actual = RuntimeInfo.Parse(parseTestCase.Raw, parseTestCase.NameProvided);

        if (parseTestCase.Raw == null)
        {
            if (parseTestCase.NameProvided == null)
            {
                Assert.Null(actual);
            }
            else
            {
                Assert.NotNull(actual);
                Assert.Equal(parseTestCase.NameProvided, actual.Name);
            }
        }
        else
        {
            Assert.NotNull(actual);
            Assert.Equal(parseTestCase.ExpectedName, actual.Name);
            Assert.Equal(parseTestCase.ExpectedVersion, actual.Version);
            Assert.Equal(parseTestCase.Raw, actual.Raw);
        }
    }

    public static IEnumerable<object[]> ParseTestCases()
    {
        yield return new object[] { new ParseTestCase
        {
            Raw = "Mono 5.10.1.47 (2017-12/8eb8f7d5e74 Fri Apr 13 20:18:12 EDT 2018)",
            ExpectedName = "Mono",
            ExpectedVersion = "5.10.1.47"
        }};
        yield return new object[] { new ParseTestCase
        {
            Raw = "5.10.1.47 (2017-12/8eb8f7d5e74 Fri Apr 13 20:18:12 EDT 2018)",
            NameProvided = "Mono",
            ExpectedName = "Mono",
            ExpectedVersion = "5.10.1.47"
        }};
        yield return new object[] { new ParseTestCase
        {
            Raw = "Mono 5.10.0 (Visual Studio built mono)",
            ExpectedName = "Mono",
            ExpectedVersion = "5.10.0"
        }};
        yield return new object[] { new ParseTestCase
        {
            Raw = ".NET Framework 4.0.30319.17020",
            ExpectedName = ".NET Framework",
            ExpectedVersion = "4.0.30319.17020"
        }};
        yield return new object[] { new ParseTestCase
        {
            Raw = "4.0.30319.17020",
            NameProvided = ".NET Framework",
            ExpectedName = ".NET Framework",
            ExpectedVersion = "4.0.30319.17020"
        }};
        yield return new object[] { new ParseTestCase
        {
            Raw = ".NET Core 1.0",
            ExpectedName = ".NET Core",
            ExpectedVersion = "1.0"
        }};
        yield return new object[] { new ParseTestCase
        {
            Raw = "1.0",
            NameProvided = ".NET Core",
            ExpectedName = ".NET Core",
            ExpectedVersion = "1.0"
        }};
        yield return new object[] { new ParseTestCase
        {
            Raw = ".NET Native 2.1.65535-preview123123",
            ExpectedName = ".NET Native",
            ExpectedVersion = "2.1.65535-preview123123"
        }};
        yield return new object[] { new ParseTestCase
        {
            Raw = ".NET Framework For Windows Mobile 10",
            ExpectedName = ".NET Framework For Windows Mobile",
            ExpectedVersion = "10"
        }};
        yield return new object[] { new ParseTestCase
        {
            Raw = ".NET Framework For Windows Mobile 10.1",
            ExpectedName = ".NET Framework For Windows Mobile",
            ExpectedVersion = "10.1"
        }};
        yield return new object[] { new ParseTestCase
        {
            Raw = "web",
            ExpectedName = "web"
        }};
        yield return new object[] { new ParseTestCase
        {
            NameProvided = "web",
            ExpectedName = "web"
        }};
        yield return new object[] { new ParseTestCase
        {
            Raw = null,
            ExpectedName = null,
            ExpectedVersion = null
        }};
        yield return new object[] { new ParseTestCase
        {
            Raw = "Mono 6.13.0 (explicit/88268f9e785)",
            ExpectedName = "Mono",
            ExpectedVersion = "6.13.0"
        }};
        yield return new object[] { new ParseTestCase
        {
            Raw = "Mono Unity IL2CPP (Jun 22 2022 18:13:02)",
            ExpectedName = "Mono Unity IL2CPP",
            ExpectedVersion = null
        }};
    }

    public class ParseTestCase
    {
        public string Raw { get; set; }
        public string NameProvided { get; set; }
        public string ExpectedName { get; set; }
        public string ExpectedVersion { get; set; }
    }
}
