namespace Sentry.Internal;

internal interface IUpdatable<in T> : IUpdatable
    where T : IUpdatable<T>
{
    void UpdateFrom(T source);
}

internal interface IUpdatable
{
    void UpdateFrom(object source);
}
