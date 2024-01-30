namespace Sentry.Testing;

public class MockClock : ISystemClock
{
    private DateTimeOffset _value;

    public MockClock(DateTimeOffset value) => _value = value;

    public MockClock() : this(DateTimeOffset.MaxValue)
    {
    }

    public DateTimeOffset GetUtcNow() => _value;

    public void SetUtcNow(DateTimeOffset value) => _value = value;
}
