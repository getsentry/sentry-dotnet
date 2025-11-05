namespace Sentry.Tests.Internals;

public class CacheDirectoryCoordinatorTests : IDisposable
{
    private readonly Fixture _fixture = new();

    public void Dispose()
    {
       _fixture.CacheRoot.Dispose();
    }

    private class Fixture
    {
        public readonly TempDirectory CacheRoot = new();
        public ReadWriteFileSystem FileSystem { get; } = new();

        public string CacheDirectoryPath { get; private set; }
        private readonly string _isolatedDirectory = Guid.NewGuid().ToString("N");

        public CacheDirectoryCoordinator GetSut()
        {
            CacheDirectoryPath = Path.Combine(CacheRoot.Path, _isolatedDirectory);
            return new(CacheDirectoryPath, null, FileSystem);
        }
    }

    [Fact]
    public void TryAcquire_FirstTime_ReturnsTrueAndCreatesLockFile()
    {
        // Arrange
        using var coordinator = _fixture.GetSut();

        // Act
        var acquired = coordinator.TryAcquire();

        // Assert
        Assert.True(acquired);
        Assert.True(_fixture.FileSystem.FileExists(_fixture.CacheDirectoryPath + ".lock"));
    }

    [Fact]
    public void TryAcquire_TwiceOnSameCoordinator_IdempotentTrue()
    {
        // Arrange
        using var coordinator = _fixture.GetSut();

        // Act
        Assert.True(coordinator.TryAcquire());
        Assert.True(coordinator.TryAcquire());
    }

    [Fact]
    public void TryAcquire_Locked_Fails()
    {
        // Arrange
        using var c1 = _fixture.GetSut();
        using var c2 = _fixture.GetSut();
        Assert.True(c1.TryAcquire());

        // Act & Assert
        Assert.False(c2.TryAcquire());
    }

    [Fact]
    public void TryAcquireMultiple_Released_Succeeds()
    {
        // Arrange
        var c1 = _fixture.GetSut();
        Assert.True(c1.TryAcquire());
        c1.Dispose();

        // Act & Assert
        using var c3 = _fixture.GetSut();
        Assert.True(c3.TryAcquire());
    }

    [Fact]
    public void TryAcquire_AfterDispose_ReturnsFalse()
    {
        // Arrange
        var coordinator = _fixture.GetSut();
        coordinator.Dispose();

        // Act & Assert
        Assert.False(coordinator.TryAcquire());
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_NoThrow()
    {
        // Arrange
        var coordinator = _fixture.GetSut();
        coordinator.TryAcquire();

        // Act & Assert
        coordinator.Dispose();
        coordinator.Dispose();
    }
}
