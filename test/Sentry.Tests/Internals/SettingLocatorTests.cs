using Sentry.Tests.Helpers.Reflection;

namespace Sentry.Tests.Internals;

public class SettingLocatorTests
{
    private const string DisableSdkDsnValue = SentryConstants.DisableSdkDsnValue;
    private const string DsnEnvironmentVariable = Constants.DsnEnvironmentVariable;
    private const string EnvironmentEnvironmentVariable = Constants.EnvironmentEnvironmentVariable;
    private const string ReleaseEnvironmentVariable = Constants.ReleaseEnvironmentVariable;

    private static Assembly GetAssemblyWithDsn(string dsn) =>
        AssemblyCreationHelper.CreateAssemblyWithDsnAttribute(dsn);

    [Fact]
    public void SetOnOptionsByDefault()
    {
        var options = new SentryOptions();
        var locator = options.SettingLocator;

        Assert.IsType<SettingLocator>(locator);
    }

    [Fact]
    public void UsesRealEnvironmentVariablesByDefault()
    {
        var tempVarName = Guid.NewGuid().ToString();
        Environment.SetEnvironmentVariable(tempVarName, "1");

        var options = new SentryOptions();
        var result = options.SettingLocator.GetEnvironmentVariable(tempVarName);

        Assert.Equal("1", result);
    }

    [Fact]
    public void CanUseFakeEnvironmentVariables()
    {
        var tempVarName = Guid.NewGuid().ToString();

        var options = new SentryOptions();
        options.FakeSettings().EnvironmentVariables[tempVarName] = "1";

        var result = options.SettingLocator.GetEnvironmentVariable(tempVarName);

        Assert.Equal("1", result);
    }

    [Fact]
    public void FakeEnvironmentVariableDoesntSetRealVariable()
    {
        var tempVarName = Guid.NewGuid().ToString();

        var options = new SentryOptions();
        options.FakeSettings().EnvironmentVariables[tempVarName] = "1";

        var result = Environment.GetEnvironmentVariable(tempVarName);

        Assert.Null(result);
    }

    [Fact]
    public void UsesEntryAssemblyByDefault()
    {
        var expected = Assembly.GetEntryAssembly();
        var options = new SentryOptions();

        var actual = options.SettingLocator.AssemblyForAttributes;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void CanUseOtherAssembly()
    {
        var expected = typeof(object).Assembly;
        var options = new SentryOptions();
        options.FakeSettings().AssemblyForAttributes = expected;

        var actual = options.SettingLocator.AssemblyForAttributes;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void GetDsn_WithEmptyString_DoesNotThrow()
    {
        var options = new SentryOptions { Dsn = DisableSdkDsnValue };

        var dsn = options.SettingLocator.GetDsn();

        Assert.Equal(DisableSdkDsnValue, dsn);
        Assert.Equal(DisableSdkDsnValue, options.Dsn);
    }

    [Fact]
    public void GetDsn_WithDsnInEnvironmentVariable_ReturnsAndSetsDsn()
    {
        var options = new SentryOptions();
        options.FakeSettings().EnvironmentVariables[DsnEnvironmentVariable] = ValidDsn;

        var dsn = options.SettingLocator.GetDsn();

        Assert.Equal(ValidDsn, dsn);
        Assert.Equal(ValidDsn, options.Dsn);
    }

    [Fact]
    public void GetDsn_WithDsnInAttribute_ReturnsAndSetsDsn()
    {
        var options = new SentryOptions();
        options.FakeSettings().AssemblyForAttributes = GetAssemblyWithDsn(ValidDsn);

        var dsn = options.SettingLocator.GetDsn();

        Assert.Equal(ValidDsn, dsn);
        Assert.Equal(ValidDsn, options.Dsn);
    }

    [Fact]
    public void GetDsn_WithDsnInBothEnvironmentVariableAndAttribute_ReturnsAndSetsDsnFromEnvironmentVariable()
    {
        const string validDsn1 = ValidDsn + "1";
        const string validDsn2 = ValidDsn + "2";

        var options = new SentryOptions();
        options.FakeSettings().EnvironmentVariables[DsnEnvironmentVariable] = validDsn1;
        options.FakeSettings().AssemblyForAttributes = GetAssemblyWithDsn(validDsn2);

        var dsn = options.SettingLocator.GetDsn();

        Assert.Equal(validDsn1, dsn);
        Assert.Equal(validDsn1, options.Dsn);
    }

    [Fact]
    public void GetDsn_DsnIsNonEmptyString_IgnoresEnvironmentVariable()
    {
        const string validDsn1 = ValidDsn + "1";
        const string validDsn2 = ValidDsn + "2";

        var options = new SentryOptions { Dsn = validDsn1 };
        options.FakeSettings().EnvironmentVariables[DsnEnvironmentVariable] = validDsn2;

        var dsn = options.SettingLocator.GetDsn();

        Assert.Equal(validDsn1, dsn);
        Assert.Equal(validDsn1, options.Dsn);
    }

    [Fact]
    public void GetDsn_DsnIsNonEmptyString_IgnoresAttribute()
    {
        const string validDsn1 = ValidDsn + "1";
        const string validDsn2 = ValidDsn + "2";

        var options = new SentryOptions { Dsn = validDsn1 };
        options.FakeSettings().AssemblyForAttributes = GetAssemblyWithDsn(validDsn2);

        var dsn = options.SettingLocator.GetDsn();

        Assert.Equal(validDsn1, dsn);
        Assert.Equal(validDsn1, options.Dsn);
    }

    [Fact]
    public void GetDsn_WithNoValueAnywhere_ThrowsException()
    {
        var options = new SentryOptions();

        Assert.Throws<ArgumentNullException>(() => options.SettingLocator.GetDsn());
    }

    [Fact]
    public void GetDsn_WithDisabledDsnInEnvironmentVariable_ReturnsAndSetsDisabledDsn()
    {
        var options = new SentryOptions();
        options.FakeSettings().EnvironmentVariables[DsnEnvironmentVariable] = DisableSdkDsnValue;

        var dsn = options.SettingLocator.GetDsn();

        Assert.Equal(DisableSdkDsnValue, dsn);
        Assert.Equal(DisableSdkDsnValue, options.Dsn);
    }

    [Fact]
    public void GetDsn_WithDisabledDsnInAttribute_ReturnsAndSetsDisabledDsn()
    {
        var options = new SentryOptions();
        options.FakeSettings().AssemblyForAttributes = GetAssemblyWithDsn(DisableSdkDsnValue);

        var dsn = options.SettingLocator.GetDsn();

        Assert.Equal(DisableSdkDsnValue, dsn);
        Assert.Equal(DisableSdkDsnValue, options.Dsn);
    }

    [Fact]
    public void GetDsn_WithDisabledDsnInEnvironmentVariableButValidDsnInAttribute_ReturnsAndSetsDisabledDsn()
    {
        var options = new SentryOptions();
        options.FakeSettings().EnvironmentVariables[DsnEnvironmentVariable] = DisableSdkDsnValue;
        options.FakeSettings().AssemblyForAttributes = GetAssemblyWithDsn(ValidDsn);

        var dsn = options.SettingLocator.GetDsn();

        Assert.Equal(DisableSdkDsnValue, dsn);
        Assert.Equal(DisableSdkDsnValue, options.Dsn);
    }

    [Fact]
    public void GetDsn_DsnIsStringEmptyButEnvironmentValid_ReturnsAndSetsEnvironmentDsn()
    {
        var options = new SentryOptions { Dsn = string.Empty };
        options.FakeSettings().EnvironmentVariables[DsnEnvironmentVariable] = ValidDsn;

        var dsn = options.SettingLocator.GetDsn();

        Assert.Equal(ValidDsn, dsn);
        Assert.Equal(ValidDsn, options.Dsn);
    }

    [Fact]
    public void GetDsn_DsnIsStringEmptyButAttributeValid_ReturnsAndSetsAttributeDsn()
    {
        var options = new SentryOptions { Dsn = string.Empty };
        options.FakeSettings().AssemblyForAttributes = GetAssemblyWithDsn(ValidDsn);

        var dsn = options.SettingLocator.GetDsn();

        Assert.Equal(ValidDsn, dsn);
        Assert.Equal(ValidDsn, options.Dsn);
    }

    [Fact]
    public void GetDsn_DsnIsStringEmptyButEnvironmentAndAttributeValid_ReturnsAndSetsEnvironmentDsn()
    {
        const string validDsn1 = ValidDsn + "1";
        const string validDsn2 = ValidDsn + "2";

        var options = new SentryOptions { Dsn = string.Empty };
        options.FakeSettings().EnvironmentVariables[DsnEnvironmentVariable] = validDsn1;
        options.FakeSettings().AssemblyForAttributes = GetAssemblyWithDsn(validDsn2);

        var dsn = options.SettingLocator.GetDsn();

        Assert.Equal(validDsn1, dsn);
        Assert.Equal(validDsn1, options.Dsn);
    }

    [Fact]
    public void GetEnvironment_WithEnvironmentInEnvironmentVariable_ReturnsAndSetsEnvironment()
    {
        var options = new SentryOptions();
        options.FakeSettings().EnvironmentVariables[EnvironmentEnvironmentVariable] = "Foo";

        var environment = options.SettingLocator.GetEnvironment();

        Assert.Equal("Foo", environment);
        Assert.Equal("Foo", options.Environment);
    }

    [Fact]
    public void GetEnvironment_WithOptionAlreadySet_IgnoresEnvironmentVariable()
    {
        var options = new SentryOptions { Environment = "Foo" };
        options.FakeSettings().EnvironmentVariables[EnvironmentEnvironmentVariable] = "Bar";

        var environment = options.SettingLocator.GetEnvironment();

        Assert.Equal("Foo", environment);
        Assert.Equal("Foo", options.Environment);
    }

    [Fact]
    public void GetEnvironment_WithNoValueAnywhere_ReturnsAndSetsDefault()
    {
        var options = new SentryOptions();

        var environment = options.SettingLocator.GetEnvironment();

        var expected = Debugger.IsAttached
            ? Internal.Constants.DebugEnvironmentSetting
            : Internal.Constants.ProductionEnvironmentSetting;

        Assert.Equal(expected, environment);
        Assert.Equal(expected, options.Environment);
    }

    [Fact]
    public void GetEnvironment_CanOptOutOfDefault()
    {
        var options = new SentryOptions();

        var environment = options.SettingLocator.GetEnvironment(useDefaultIfNotFound: false);

        Assert.Null(environment);
        Assert.Null(options.Environment);
    }

    [Fact]
    public void GetRelease_WithEnvironmentVariable_ReturnsAndSetsRelease()
    {
        var options = new SentryOptions();
        options.FakeSettings().EnvironmentVariables[ReleaseEnvironmentVariable] = "1.2.3";

        var release = options.SettingLocator.GetRelease();

        Assert.Equal("1.2.3", release);
        Assert.Equal("1.2.3", options.Release);
    }

    [Fact]
    public void GetRelease_WithOptionAlreadySet_IgnoresEnvironmentVariable()
    {
        var options = new SentryOptions { Release = "1.2.3" };
        options.FakeSettings().EnvironmentVariables[ReleaseEnvironmentVariable] = "4.5.6";

        var release = options.SettingLocator.GetRelease();

        Assert.Equal("1.2.3", release);
        Assert.Equal("1.2.3", options.Release);
    }

    [Fact]
    public void GetRelease_WithNoValueAnywhere_ReturnsAndSetsDefault()
    {
        var assembly = typeof(SentrySdk).Assembly;
        var options = new SentryOptions();
        options.FakeSettings().AssemblyForAttributes = assembly;

        var release = options.SettingLocator.GetRelease();

        var expected = ApplicationVersionLocator.GetCurrent(assembly);

        Assert.Equal(expected, release);
        Assert.Equal(expected, options.Release);
    }
}
