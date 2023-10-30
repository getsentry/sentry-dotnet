namespace Sentry.Testing;

// TODO: Do we need this anymore? We can't use XUnit for AOT Verify tests anyway...
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class UniqueForAotAttribute : Attribute
{
    public UniqueForAotAttribute()
    {
    }
}
