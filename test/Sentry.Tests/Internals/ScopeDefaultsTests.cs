using static Sentry.Internal.Constants;

namespace Sentry.Tests.Internals;

public class ScopeDefaultsTests
{
    [Fact]
    public void Ctor_OptionsSet_UsesOptions()
    {
        var options = new SentryOptions
        {
            Release = "options-release",
            Distribution = "options-dist",
            Environment = "options-environment",
            DefaultTags = { ["key"] = "value" }
        };

        var sut = new ScopeDefaults(options);

        sut.Release.Should().Be("options-release");
        sut.Distribution.Should().Be("options-dist");
        sut.Environment.Should().Be("options-environment");
        sut.Tags.Should().Equal(new Dictionary<string, string> { ["key"] = "value" });
    }

    [Fact]
    public void Ctor_EnvironmentVariablesSet_UsesEnvironmentVariables()
    {
        var options = new SentryOptions();
        options.FakeSettings().EnvironmentVariables[ReleaseEnvironmentVariable] = "env-release";
        options.FakeSettings().EnvironmentVariables[EnvironmentEnvironmentVariable] = "env-environment";

        var sut = new ScopeDefaults(options);

        sut.Release.Should().Be("env-release");
        sut.Environment.Should().Be("env-environment");
    }

    [Fact]
    public void Ctor_OptionsChangedAfterwards_KeepsOriginalValues()
    {
        var options = new SentryOptions { Release = "original", DefaultTags = { ["key"] = "original" } };
        var sut = new ScopeDefaults(options);

        options.Release = "changed";
        options.DefaultTags["key"] = "changed";

        sut.Release.Should().Be("original");
        sut.Tags["key"].Should().Be("original");
    }

    [Fact]
    public void Sdk_NoInitPath_IsCoreSdk()
    {
        var sut = new ScopeDefaults(new SentryOptions());

        sut.Sdk.Name.Should().Be(SdkName);
        sut.Sdk.Version.Should().Be(SdkVersion.Instance.Version);
        sut.Sdk.Packages.Should().ContainSingle(p => p.Name == "nuget:" + SdkVersion.Instance.Name);
    }

    [Fact]
    public void Sdk_InitPathSetsSdk_UsesInitPathAndKeepsCorePackage()
    {
        var options = new SentryOptions();
        options.SetSdk("sentry.dotnet.test", new SdkVersion { Name = "Sentry.Test", Version = "1.2.3" });

        var sut = new ScopeDefaults(options);

        sut.Sdk.Name.Should().Be("sentry.dotnet.test");
        sut.Sdk.Version.Should().Be("1.2.3");
        sut.Sdk.Packages.Select(p => p.Name).Should().BeEquivalentTo("nuget:Sentry.Test", "nuget:" + SdkVersion.Instance.Name);
    }

    [Fact]
    public void GetEnvironment_ScopeOverrides_UsesScope()
    {
        var options = new SentryOptions { Environment = "default" };
        var sut = new ScopeDefaults(options);

        sut.GetEnvironment(new Scope(options) { Environment = "override" }).Should().Be("override");
        sut.GetEnvironment(new Scope(options)).Should().Be("default");
        sut.GetEnvironment(null).Should().Be("default");
    }

    [Fact]
    public void GetSdk_ScopeHasSdkName_UsesScope()
    {
        var options = new SentryOptions();
        var sut = new ScopeDefaults(options);
        var scope = new Scope(options) { Sdk = { Name = "scope-sdk" } };

        sut.GetSdk(scope).Should().BeSameAs(scope.Sdk);
        sut.GetSdk(new Scope(options)).Should().BeSameAs(sut.Sdk);
    }

    [Fact]
    public void Apply_EmptyEvent_FillsDefaults()
    {
        var options = new SentryOptions
        {
            Release = "release",
            Distribution = "dist",
            Environment = "environment",
            DefaultTags = { ["key"] = "value" }
        };
        var sut = new ScopeDefaults(options);
        var @event = new SentryEvent();

        sut.Apply(@event);

        @event.Release.Should().Be("release");
        @event.Distribution.Should().Be("dist");
        @event.Environment.Should().Be("environment");
        @event.Sdk.Name.Should().Be(SdkName);
        @event.Tags.Should().Contain("key", "value");
        @event.Contexts.Runtime.Name.Should().NotBeNull();
    }

    [Fact]
    public void Apply_EventHasValues_KeepsEventValues()
    {
        var options = new SentryOptions
        {
            Release = "release",
            Distribution = "dist",
            Environment = "environment",
            DefaultTags = { ["key"] = "value" }
        };
        var sut = new ScopeDefaults(options);
        var @event = new SentryEvent
        {
            Release = "event-release",
            Distribution = "event-dist",
            Environment = "event-environment",
            Sdk = { Name = "event-sdk" }
        };
        @event.SetTag("key", "event-value");
        @event.Contexts.Runtime.Name = "event-runtime";

        sut.Apply(@event);

        @event.Release.Should().Be("event-release");
        @event.Distribution.Should().Be("event-dist");
        @event.Environment.Should().Be("event-environment");
        @event.Sdk.Name.Should().Be("event-sdk");
        @event.Tags["key"].Should().Be("event-value");
        @event.Contexts.Runtime.Name.Should().Be("event-runtime");
        @event.Contexts.Runtime.Version.Should().Be(sut.Contexts.Runtime.Version);
    }

    [Fact]
    public void Apply_Transaction_FillsReleaseAndDistribution()
    {
        var options = new SentryOptions { Release = "release", Distribution = "dist" };
        var sut = new ScopeDefaults(options);
        var transaction = new SentryTransaction("name", "operation");

        sut.Apply(transaction);

        transaction.Release.Should().Be("release");
        transaction.Distribution.Should().Be("dist");
    }

    [Fact]
    public void Apply_TwoEvents_DoNotShareContexts()
    {
        var sut = new ScopeDefaults(new SentryOptions());
        var first = new SentryEvent();
        var second = new SentryEvent();

        sut.Apply(first);
        sut.Apply(second);
        first.Contexts.Runtime.Name = "changed";

        second.Contexts.Runtime.Name.Should().Be(sut.Contexts.Runtime.Name);
    }

    [Fact]
    public void Apply_CheckIn_UsesScopeEnvironmentOverride()
    {
        var options = new SentryOptions { Release = "release", Environment = "default" };
        var sut = new ScopeDefaults(options);
        var checkIn = new SentryCheckIn("slug", CheckInStatus.InProgress);

        sut.Apply(checkIn, new Scope(options) { Environment = "override" });

        checkIn.Release.Should().Be("release");
        checkIn.Environment.Should().Be("override");
    }
}
