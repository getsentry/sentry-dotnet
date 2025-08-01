using Sentry.Threading;

namespace Sentry.Tests.Threading;

public class ScopedCountdownLockTests : IDisposable
{
    private readonly ScopedCountdownLock _lock = new();

    public void Dispose()
    {
        _lock.Dispose();
    }

    [Fact]
    public void Init_IsNotEngaged_IsNotSet()
    {
        AssertDisengaged(false, 0);
    }

    [Fact]
    public void TryEnterCounterScope_IsNotEngaged_IsNotSet()
    {
        // increment the count
        var counterOne = _lock.TryEnterCounterScope();
        counterOne.IsEntered.Should().BeTrue();
        AssertDisengaged(false, 1);

        // increment the count
        var counterTwo = _lock.TryEnterCounterScope();
        counterTwo.IsEntered.Should().BeTrue();
        AssertDisengaged(false, 2);

        // decrement the count
        counterOne.Dispose();
        counterOne.IsEntered.Should().BeFalse();
        AssertDisengaged(false, 1);

        // decrement the count
        counterTwo.Dispose();
        counterTwo.IsEntered.Should().BeFalse();
        AssertDisengaged(false, 0);

        // no-op ... already disposed
        counterOne.Dispose();
        counterTwo.Dispose();
        AssertDisengaged(false, 0);

        // increment the count
        var counterThree = _lock.TryEnterCounterScope();
        counterThree.IsEntered.Should().BeTrue();
        AssertDisengaged(false, 1);

        // decrement the count
        counterThree.Dispose();
        counterThree.IsEntered.Should().BeFalse();
        AssertDisengaged(false, 0);
    }

    [Fact]
    public void TryEnterLockScope_IsEngaged_IsSet()
    {
        // successfully enter a CounterScope ... increment the count
        var counterOne = _lock.TryEnterCounterScope();
        counterOne.IsEntered.Should().BeTrue();
        AssertDisengaged(false, 1);

        // successfully enter a LockScope ... engages the lock
        var lockOne = _lock.TryEnterLockScope();
        lockOne.IsEntered.Should().BeTrue();
        AssertEngaged(false, 1);

        // cannot enter another LockScope as long as the lock is already engaged by a LockScope
        var lockTwo = _lock.TryEnterLockScope();
        lockTwo.IsEntered.Should().BeFalse();
        AssertEngaged(false, 1);

        // no-op ... LockScope is not entered
        lockTwo.Wait();
        lockTwo.Dispose();
        AssertEngaged(false, 1);

        // successfully enter another CounterScope ... lock is engaged but not yet set
        var counterTwo = _lock.TryEnterCounterScope();
        counterTwo.IsEntered.Should().BeTrue();
        AssertEngaged(false, 2);

        // exit a CounterScope ... decrement the count
        counterTwo.Dispose();
        AssertEngaged(false, 1);

        // exit last CounterScope ... count of engaged lock reaches zero ... sets the lock
        counterOne.Dispose();
        AssertEngaged(true, 0);

        // cannot enter another CounterScope as long as the engaged lock is set
        var counterThree = _lock.TryEnterCounterScope();
        counterThree.IsEntered.Should().BeFalse();
        AssertEngaged(true, 0);
        counterThree.Dispose();
        AssertEngaged(true, 0);

        // would block if the count of the engaged lock was not zero
        lockOne.Wait();

        // exit the LockScope ... reset the lock
        lockOne.Dispose();
        AssertDisengaged(false, 0);

        // can enter a CounterScope again ... the lock not set
        var counterFour = _lock.TryEnterCounterScope();
        counterFour.IsEntered.Should().BeTrue();
        AssertDisengaged(false, 1);
        counterFour.Dispose();
        AssertDisengaged(false, 0);
    }

    [Fact]
    public void Dispose_UseAfterDispose_Throws()
    {
        _lock.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _lock.TryEnterCounterScope());
        Assert.Throws<ObjectDisposedException>(() => _lock.TryEnterLockScope());
    }

    private void AssertEngaged(bool isSet, int count)
    {
        using (new AssertionScope())
        {
            _lock.IsSet.Should().Be(isSet);
            _lock.Count.Should().Be(count);
            _lock.IsEngaged.Should().BeTrue();
        }
    }

    private void AssertDisengaged(bool isSet, int count)
    {
        using (new AssertionScope())
        {
            _lock.IsSet.Should().Be(isSet);
            _lock.Count.Should().Be(count);
            _lock.IsEngaged.Should().BeFalse();
        }
    }
}
