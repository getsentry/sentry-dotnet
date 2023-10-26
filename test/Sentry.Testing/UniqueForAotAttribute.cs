namespace Sentry.Testing;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class UniqueForAotAttribute : Attribute
{
    public UniqueForAotAttribute()
    {
    }
}
