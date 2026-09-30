namespace Sentry.NLog.Tests;

public class SentryTargetConfigurationBindingTests
{
    private static Exception LoadConfiguration(string targetAttributes)
    {
        var configXml = $@"
            <nlog throwConfigExceptions='true'>
                <extensions><add type='{typeof(SentryTarget).AssemblyQualifiedName}' /></extensions>
                <targets><target type='Sentry' name='sentry' {targetAttributes} /></targets>
                <rules><logger name='*' writeTo='sentry' /></rules>
            </nlog>";

        var logFactory = new LogFactory();
        return Record.Exception(() => logFactory.Configuration =
            new XmlLoggingConfiguration(XmlReader.Create(new StringReader(configXml)), null, logFactory));
    }

    [Theory]
    [InlineData("dsn='https://key@sentry.io/1'")]
    [InlineData("initializeSdk='true'")]
    public void LoadConfiguration_WithSdkSetting_Throws(string targetAttributes)
    {
        var exception = LoadConfiguration(targetAttributes);

        Assert.NotNull(exception);
        Assert.Contains("SentrySdk.Init", exception.ToString());
    }

    [Fact]
    public void LoadConfiguration_WithTargetSettings_DoesNotThrow()
    {
        Assert.Null(LoadConfiguration("minimumEventLevel='Warn' includeEventPropertiesAsTags='true'"));
    }

    [Fact]
    public void LoadConfiguration_WithInitializeSdkFalse_DoesNotThrow()
    {
        Assert.Null(LoadConfiguration("initializeSdk='false'"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void AddSentry_DsnOverload_InvokedByName_Throws(int parameterCount)
    {
        var method = DsnOverload(parameterCount);

        var arguments = method.GetParameters()
            .Select(p => (object)(p.Name switch
            {
                "configuration" => new LoggingConfiguration(),
                "dsn" => ValidDsn,
                "targetName" => "sentry",
                _ => p.HasDefaultValue ? p.DefaultValue : null
            })!)
            .ToArray();

        var exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, arguments));

        Assert.IsType<NotSupportedException>(exception.InnerException);
        Assert.Contains("SentrySdk.Init", exception.InnerException!.Message);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void AddSentry_DsnOverload_IsObsoleteAsError(int parameterCount)
    {
        var obsolete = DsnOverload(parameterCount).GetCustomAttribute<ObsoleteAttribute>();

        Assert.NotNull(obsolete);
        Assert.True(obsolete!.IsError);
    }

    [Fact]
    public void Dsn_WhenSet_WritesMigrationMessageToStandardError()
    {
        var standardError = new List<string>();

        Set(GetTarget(standardError), "Dsn", (Layout)ValidDsn);

        Assert.Contains(ConfigurationExtensions.ObsoleteDsnOverload, Assert.Single(standardError));
    }

    [Fact]
    public void InitializeSdk_WhenSetToTrue_WritesMigrationMessageToStandardError()
    {
        var standardError = new List<string>();

        Set(GetTarget(standardError), "InitializeSdk", true);

        Assert.Contains(ConfigurationExtensions.ObsoleteDsnOverload, Assert.Single(standardError));
    }

    [Fact]
    public void InitializeSdk_WhenSetToFalse_WritesNothing()
    {
        var standardError = new List<string>();

        Set(GetTarget(standardError), "InitializeSdk", false);

        Assert.Empty(standardError);
    }

    [Fact]
    public void ReportUnsupportedSdkSetting_BothSettingsOnOneTarget_WritesOnce()
    {
        var standardError = new List<string>();
        var target = GetTarget(standardError);

        Set(target, "Dsn", (Layout)ValidDsn);
        Set(target, "InitializeSdk", true);

        Assert.Single(standardError);
    }

    private static SentryTarget GetTarget(List<string> standardError) =>
        new(new SentryNLogOptions(), () => Substitute.For<IHub>(), new MockClock(),
            new UninitializedSdkWarning { WriteToStandardError = standardError.Add });

    private static void Set(SentryTarget target, string property, object value) =>
        Record.Exception(() => typeof(SentryTarget).GetProperty(property)!.SetValue(target, value));

    private static MethodInfo DsnOverload(int parameterCount) => typeof(ConfigurationExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(ConfigurationExtensions.AddSentry)
                     && m.GetParameters().Any(p => p.Name == "dsn")
                     && m.GetParameters().Length == parameterCount + 1);
}
