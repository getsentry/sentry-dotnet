using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Fact]
    public void TapGestureRecognizer_LifecycleEvents_AddsBreadcrumb()
    {
        var gesture = new TapGestureRecognizer();
        TestGestureRecognizer(
            gesture,
            nameof(TapGestureRecognizer.Tapped),
            new TappedEventArgs(gesture)
        );
    }


    [Fact]
    public void SwipeGestureRecognizer_LifecycleEvents_AddsBreadcrumb()
    {
        var gesture = new SwipeGestureRecognizer();
        TestGestureRecognizer(
            gesture,
            nameof(SwipeGestureRecognizer.Swiped),
            new SwipedEventArgs(gesture, SwipeDirection.Down)
        );
    }

    [Fact]
    public void PinchGestureRecognizer_LifecycleEvents_AddsBreadcrumb()
    {
        TestGestureRecognizer(
            new PinchGestureRecognizer(),
            nameof(PinchGestureRecognizer.PinchUpdated),
            new PinchGestureUpdatedEventArgs(GestureStatus.Completed, 0, Point.Zero)
        );
    }

    [Theory]
    [InlineData(nameof(DragGestureRecognizer.DragStarting))]
    [InlineData(nameof(DragGestureRecognizer.DropCompleted))]
    public void DragGestureRecognizer_LifecycleEvents_AddsBreadcrumb(string eventName)
    {
        TestGestureRecognizer(
            new DragGestureRecognizer(),
            eventName,
            DragStartingEventArgs.Empty
        );
    }

    [Fact]
    public void PanGestureRecognizer_LifecycleEvents_AddsBreadcrumb()
    {
        TestGestureRecognizer(
            new PanGestureRecognizer(),
            nameof(PanGestureRecognizer.PanUpdated),
            new PanUpdatedEventArgs(GestureStatus.Completed, 1, 0, 0)
        );
    }

    [Theory]
    [InlineData(nameof(PointerGestureRecognizer.PointerEntered))]
    [InlineData(nameof(PointerGestureRecognizer.PointerExited))]
    [InlineData(nameof(PointerGestureRecognizer.PointerMoved))]
    [InlineData(nameof(PointerGestureRecognizer.PointerPressed))]
    [InlineData(nameof(PointerGestureRecognizer.PointerReleased))]
    public void PointerGestureRecognizer_LifecycleEvents_AddsBreadcrumb(string eventName)
    {
        TestGestureRecognizer(
            new PointerGestureRecognizer(),
            eventName,
            PointerEventArgs.Empty
        );
    }


    private void TestGestureRecognizer(GestureRecognizer gesture, string eventName, object eventArgs)
    {
        var image = new Image();
        image.GestureRecognizers.Add(gesture);

        _fixture.Binder.HandleElementEvents(image);

        // Act
        gesture.RaiseEvent(eventName, eventArgs);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{gesture.GetType().Name}.{eventName}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.LifecycleCategory, crumb.Category);
        crumb.Data.Should().Contain($"{gesture.GetType().Name}.Name", "tapgesturerecognizer");
    }
}
