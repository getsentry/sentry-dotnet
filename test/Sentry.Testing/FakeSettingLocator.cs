using System.ComponentModel;
using System.Reflection;

namespace Sentry.Testing;

internal class FakeSettingLocator : SettingLocator
{
    public FakeSettingLocator(SentryOptions options) : base(options)
    {
    }

    public Dictionary<string, string> EnvironmentVariables { get; } = new();

    public new Assembly AssemblyForAttributes
    {
        get => base.AssemblyForAttributes;
        set => base.AssemblyForAttributes = value;
    }

    public override string GetEnvironmentVariable(string variable) =>
        EnvironmentVariables.TryGetValue(variable, out var value) ? value : null;
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal static class SettingsLocatorExtension
{
    internal static FakeSettingLocator FakeSettings(this SentryOptions options)
    {
        if (options.SettingLocator is FakeSettingLocator locator)
        {
            return locator;
        }

        locator = new(options);
        options.SettingLocator = locator;
        return locator;
    }
}
