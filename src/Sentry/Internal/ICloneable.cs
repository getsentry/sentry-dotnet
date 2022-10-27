namespace Sentry.Internal;

internal interface ICloneable<out T>
{
    T Clone();
}
