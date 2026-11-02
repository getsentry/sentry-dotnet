namespace Sentry.Tests.Protocol.Envelopes;

public class BufferedStreamSerializableTests
{
    [Fact]
    public async Task SerializeAsync_CancelledWaiter_DoesNotCancelSharedBuffer()
    {
        // Arrange
        const string attachmentContent = "test attachment content";
        using var source = new GatedMemoryStream(Encoding.UTF8.GetBytes(attachmentContent));
        using var serializable = new BufferedStreamSerializable(source);
        using var cancelledOutput = new MemoryStream();
        using var successfulOutput = new MemoryStream();
        using var cancellationSource = new CancellationTokenSource();

        // Act
        var cancelledSerialization = serializable.SerializeAsync(
            cancelledOutput,
            null,
            cancellationSource.Token);

        await source.CopyStarted.WaitAsync(TimeSpan.FromSeconds(5));

        var successfulSerialization = serializable.SerializeAsync(
            successfulOutput,
            null);

        cancellationSource.Cancel();

        var completedTask = await Task.WhenAny(
            cancelledSerialization,
            Task.Delay(TimeSpan.FromSeconds(1)));

        source.Release();

        await successfulSerialization;

        // Assert
        completedTask.Should().BeSameAs(cancelledSerialization);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cancelledSerialization);

        Encoding.UTF8.GetString(successfulOutput.ToArray())
            .Should().Be(attachmentContent);
    }

    [Fact]
    public async Task SerializeAsync_SourceThrows_DisposesSourceAndPropagatesException()
    {
        // Arrange
        using var source = new ThrowingReadStream();
        using var serializable = new BufferedStreamSerializable(source);
        using var output = new MemoryStream();

        // Act
        Func<Task> action = () => serializable.SerializeAsync(output, null);

        // Assert
        await action.Should()
            .ThrowAsync<IOException>()
            .WithMessage("Test exception.");

        source.WasDisposed.Should().BeTrue();
    }

    private sealed class GatedMemoryStream : MemoryStream
    {
        private readonly TaskCompletionSource<bool> _copyStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<bool> _continueCopy =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public GatedMemoryStream(byte[] buffer)
            : base(buffer)
        {
        }

        public Task CopyStarted => _copyStarted.Task;

        public void Release() => _continueCopy.TrySetResult(true);

        public override async Task CopyToAsync(
            Stream destination,
            int bufferSize,
            CancellationToken cancellationToken)
        {
            _copyStarted.TrySetResult(true);

            await _continueCopy.Task.ConfigureAwait(false);
            await base.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class ThrowingReadStream : MemoryStream
    {
        public bool WasDisposed { get; private set; }

        public override Task CopyToAsync(
            Stream destination,
            int bufferSize,
            CancellationToken cancellationToken) =>
            Task.FromException(new IOException("Test exception."));

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                WasDisposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
