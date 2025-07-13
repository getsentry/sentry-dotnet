using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public class MauiGestureRecognizerEventsBinderTests
{
    private readonly MauiEventsBinderFixture _fixture = new(new MauiGestureRecognizerEventsBinder());

    [SkippableFact]
    public void TapGestureRecognizer_LifecycleEvents_AddsBreadcrumb()
    {
#if !VISUAL_RUNNER
        Skip.If(true, "Visual runner disabled");
#endif
        var gesture = new TapGestureRecognizer();
        TestGestureRecognizer(
            gesture,
            nameof(TapGestureRecognizer.Tapped),
            new TappedEventArgs(gesture)
        );
    }

    [SkippableFact]
    public void SwipeGestureRecognizer_LifecycleEvents_AddsBreadcrumb()
    {
#if !VISUAL_RUNNER
        Skip.If(true, "Visual runner disabled");
#endif
        var gesture = new SwipeGestureRecognizer();
        TestGestureRecognizer(
            gesture,
            nameof(SwipeGestureRecognizer.Swiped),
            new SwipedEventArgs(gesture, SwipeDirection.Down)
        );
    }

    [SkippableFact]
    public void PinchGestureRecognizer_LifecycleEvents_AddsBreadcrumb()
    {
#if !VISUAL_RUNNER
        Skip.If(true, "Visual runner disabled");
#endif
        TestGestureRecognizer(
            new PinchGestureRecognizer(),
            nameof(PinchGestureRecognizer.PinchUpdated),
            new PinchGestureUpdatedEventArgs(GestureStatus.Completed, 0, Point.Zero)
        );
    }

    [SkippableFact]
    public void DragGestureRecognizer_LifecycleEvents_AddsBreadcrumb_DragStarting()
    {
#if !VISUAL_RUNNER
        Skip.If(true, "Visual runner disabled");
#endif
        TestGestureRecognizer(
            new DragGestureRecognizer(),
            nameof(DragGestureRecognizer.DragStarting),
            new DragStartingEventArgs()
        );
    }

    [SkippableFact]
    public void DragGestureRecognizer_LifecycleEvents_AddsBreadcrumb_DropFinished()
    {
#if !VISUAL_RUNNER
        Skip.If(true, "Visual runner disabled");
#endif
        TestGestureRecognizer(
            new DragGestureRecognizer(),
            nameof(DragGestureRecognizer.DropCompleted),
            new DropCompletedEventArgs()
        );
    }

    [SkippableFact]
    public void PanGestureRecognizer_LifecycleEvents_AddsBreadcrumb()
    {
#if !VISUAL_RUNNER
        Skip.If(true, "Visual runner disabled");
#endif
        TestGestureRecognizer(
            new PanGestureRecognizer(),
            nameof(PanGestureRecognizer.PanUpdated),
            new PanUpdatedEventArgs(GestureStatus.Completed, 1, 0, 0)
        );
    }

    [SkippableTheory]
    [InlineData(nameof(PointerGestureRecognizer.PointerEntered))]
    [InlineData(nameof(PointerGestureRecognizer.PointerExited))]
    [InlineData(nameof(PointerGestureRecognizer.PointerMoved))]
    [InlineData(nameof(PointerGestureRecognizer.PointerPressed))]
    [InlineData(nameof(PointerGestureRecognizer.PointerReleased))]
    public void PointerGestureRecognizer_LifecycleEvents_AddsBreadcrumb(string eventName)
    {
#if !VISUAL_RUNNER
        Skip.If(true, "Visual runner disabled");
#endif
        TestGestureRecognizer(
            new PointerGestureRecognizer(),
            eventName,
            new PointerEventArgs()
        );
    }

    private void TestGestureRecognizer(GestureRecognizer gesture, string eventName, object eventArgs)
    {
        var image = new Image();
        image.GestureRecognizers.Add(gesture);
        var args = new ElementEventArgs(image);

        try
        {
            _fixture.Binder.OnApplicationOnDescendantAdded(null, args);

            // Act
            gesture.RaiseEvent(eventName, eventArgs);

            // Assert
            var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
            Assert.Equal($"{gesture.GetType().Name}.{eventName}", crumb.Message);
            Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
            Assert.Equal(MauiEventsBinder.UserType, crumb.Type);
            Assert.Equal(MauiEventsBinder.UserActionCategory, crumb.Category);
            _fixture.Binder.OnApplicationOnDescendantRemoved(null, args);
        }
        catch
        {
            _fixture.Binder.OnApplicationOnDescendantRemoved(null, args);
            throw;
        }
        // GC.WaitForPendingFinalizers();
    }
}
