using Foundation;
using Microsoft.Maui.LifecycleEvents;
using Sentry.Maui.Internal;
using Sentry.Maui.Tests.Mocks;

namespace Sentry.Maui.Tests;

public partial class SentryMauiAppBuilderExtensionsTests
{
    [Fact]
    public void UseSentry_BindsToApplicationStartupEvent_iOS()
    {
        // Arrange
        var application = MockApplication.Create();
        var binder = Substitute.For<MauiEventsBinder>();

        var builder = _fixture.Builder;
        builder.Services.AddSingleton<IApplication>(application);
        builder.Services.AddSingleton(binder);
        builder.UseSentry(ValidDsn);
        using var app = builder.Build();

        var iosApplication = new MockIosApplication(application, app.Services);

        // A bit of hackery here, because we can't mock UIKit.UIApplication.
        var launchOptions = NSDictionary.FromObjectAndKey(iosApplication, new NSString("application"));

        // Act
        var lifecycleEventService = app.Services.GetRequiredService<ILifecycleEventService>();
        lifecycleEventService.InvokeEvents<iOSLifecycle.WillFinishLaunching>
            (nameof(iOSLifecycle.WillFinishLaunching), del =>
                del.Invoke(null!, launchOptions));

        // Assert
        binder.Received(1).HandleApplicationEvents(application);
    }

    private class MockIosApplication : NSObject, IPlatformApplication
    {
        public MockIosApplication(IApplication application, IServiceProvider services)
        {
            Application = application;
            Services = services;
        }

        public IApplication Application { get; }

        public IServiceProvider Services { get; }
    }
}
