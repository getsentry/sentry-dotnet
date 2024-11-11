namespace Sentry.Tests.Internals;

public class GarbageCollectionMonitorTests
{
    [SkippableFact]
    public async Task MonitorGarbageCollection_TaskCancelled_CancelsFullGCNotification()
    {
        // Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "These tests may be hanging in CI on Windows");

        // Arrange
        var reset = new ManualResetEventSlim(false);
        var gc = Substitute.For<IGCImplementation>();
        gc.When(x => x.RegisterForFullGCNotification(Arg.Any<int>(), Arg.Any<int>()))
          .Do(_ => reset.Set());
        gc.WaitForFullGCComplete(Arg.Any<TimeSpan>()).Returns(GCNotificationStatus.Succeeded);

        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        var task = GarbageCollectionMonitor.Start(() => { }, cancellationTokenSource.Token, gc);
        reset.Wait(); // Wait until the task is running
        await cancellationTokenSource.CancelAsync();
        await task;

        // Assert
        task.Status.Should().Be(TaskStatus.RanToCompletion);
        gc.Received(1).CancelFullGCNotification();
    }

    [SkippableFact]
    public async Task MonitorGarbageCollection_WaitForFullGCCompleteSucceeds_InvokesOnGarbageCollected()
    {
        // Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "These tests may be hanging in CI on Windows");

        // Arrange
        var reset = new ManualResetEventSlim(false);
        var onGarbageCollected = Substitute.For<Action>();
        var gc = Substitute.For<IGCImplementation>();
        gc.When(x => x.RegisterForFullGCNotification(Arg.Any<int>(), Arg.Any<int>()))
          .Do(_ => reset.Set());
        gc.WaitForFullGCComplete(Arg.Any<TimeSpan>()).Returns(GCNotificationStatus.Succeeded);

        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        var task = GarbageCollectionMonitor.Start(onGarbageCollected, cancellationTokenSource.Token, gc);
        reset.Wait(); // Wait until the task is running
        await Task.Delay(100); // Give it some time to invoke the callback

        await cancellationTokenSource.CancelAsync();
        await task;

        // Assert
        onGarbageCollected.Received(1);
    }

    [SkippableFact]
    public async Task MonitorGarbageCollection_GCException_Throws()
    {
        // Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "These tests may be hanging in CI on Windows");

        // Arrange
        var onGarbageCollected = Substitute.For<Action>();
        var gc = Substitute.For<IGCImplementation>();
        gc.When(x => x.RegisterForFullGCNotification(Arg.Any<int>(), Arg.Any<int>()))
          .Do(_ => throw new Exception());

        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        var task = GarbageCollectionMonitor.Start(onGarbageCollected, cancellationTokenSource.Token, gc);
        var timeout = Task.Delay(5000, cancellationTokenSource.Token);
        await Task.WhenAny([task, timeout]);

        // Assert
        task.Status.Should().Be(TaskStatus.Faulted);
        task.Exception.Should().NotBeNull();
    }
}
