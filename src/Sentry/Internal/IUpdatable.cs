namespace Sentry.Internal;

internal interface IUpdatable<in T> : IUpdatable
    where T : IUpdatable<T>
{
    public void UpdateFrom(T source);
}

internal interface IUpdatable
{
    public void UpdateFrom(object source);
}
