using Foundation;
using Microsoft.Maui.LifecycleEvents;
using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public partial class SentryMauiAppBuilderExtensionsTests
{
    [Fact]
    public void UseSentry_BindsToLifecycleEvents_iOS()
    {
        // Arrange
        var binder = Substitute.For<IMauiEventsBinder>();

        var builder = _fixture.Builder;
        builder.Services.AddSingleton(binder);

        // Act
        builder.UseSentry(ValidDsn);
        using var app = builder.Build();

        // A bit of hackery here, because we can't mock UIKit.UIApplication.
        var application = new FakeIosApplication(app.Services);
        var launchOptions = NSDictionary.FromObjectAndKey(application, (NSString)nameof(application));

        var lifecycleEventService = app.Services.GetRequiredService<ILifecycleEventService>();
        lifecycleEventService.InvokeEvents<iOSLifecycle.WillFinishLaunching>
            (nameof(iOSLifecycle.WillFinishLaunching), del =>
                del.Invoke(null!, launchOptions));

        // Assert
        binder.Received(1).BindMauiEvents();
    }

    private class FakeIosApplication : NSObject, IPlatformApplication
    {
        public FakeIosApplication(IServiceProvider services)
        {
            Services = services;
        }

        public IServiceProvider Services { get; }
        public IApplication Application => null;
    }
}
