namespace Sentry.Internal;

internal interface ICloneable<out T>
{
    public T Clone();
}
