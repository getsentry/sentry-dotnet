using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    private readonly MauiEventsBinderFixture _fixture = new();

    // Most of the tests for this class are in separate partial class files for better organisation

    [Fact]
    public void OnBreadcrumbCreateCallback_CreatesBreadcrumb()
    {
        // Arrange
        var breadcrumbEvent = new BreadcrumbEvent(new object(), "TestName",
            ("key1", "value1"), ("key2", "value2")
            );

        // Act
        _fixture.Binder.OnBreadcrumbCreateCallback(breadcrumbEvent);

        // Assert
        using (new AssertionScope())
        {
            var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
            Assert.Equal("Object.TestName", crumb.Message);
            Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
            Assert.Equal(MauiEventsBinder.UserType, crumb.Type);
            Assert.Equal(MauiEventsBinder.UserActionCategory, crumb.Category);
            Assert.NotNull(crumb.Data);
            Assert.Equal(breadcrumbEvent.ExtraData.Count(), crumb.Data.Count);
            foreach (var (key, value) in breadcrumbEvent.ExtraData)
            {
                crumb.Data.Should().Contain(kvp => kvp.Key == key && kvp.Value == value);
            }
        }
    }

    [Fact]
    public void ElementEventBinders_EnabledOnly()
    {
        // Arrange
        var options1 = new SentryMauiOptions { Dsn = ValidDsn };
#if __ANDROID__
        options1.Native.ExperimentalOptions.SessionReplay.MaskControlsOfType<object>(); // force masking to be enabled
        options1.Native.ExperimentalOptions.SessionReplay.SessionSampleRate = 1.0;
        options1.Native.ExperimentalOptions.SessionReplay.OnErrorSampleRate = 1.0;
#endif
        var enabledBinder = new MauiSessionReplayMaskControlsOfTypeBinder(options1);

        var options2 = new SentryMauiOptions { Dsn = ValidDsn };
#if __ANDROID__
        options2.Native.ExperimentalOptions.SessionReplay.SessionSampleRate = 0.0;
        options2.Native.ExperimentalOptions.SessionReplay.OnErrorSampleRate = 0.0;
#endif
        var disabledBinder = new MauiSessionReplayMaskControlsOfTypeBinder(options2);

        var buttonEventBinder = new MauiButtonEventsBinder();

        // Act
        var fixture = new MauiEventsBinderFixture(buttonEventBinder, enabledBinder, disabledBinder);

        // Assert
#if __ANDROID__
        var expectedBinders = new List<IMauiElementEventBinder> { buttonEventBinder, enabledBinder };
#else
        // We only register MauiSessionReplayMaskControlsOfTypeBinder on platforms that support Session Replay
        var expectedBinders = new List<IMauiElementEventBinder> { buttonEventBinder};
#endif
        fixture.Binder._elementEventBinders.Should().BeEquivalentTo(expectedBinders);
    }
}
