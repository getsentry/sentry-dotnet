namespace Sentry.Testing;

public static class HubSubstituteExtensions
{
    public static void SubstituteConfigureScope(
        this IHub hub,
        Scope scope)
    {
        hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
           .Do(c => c.Arg<Action<Scope>>().Invoke(scope));

        hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope, Arg.AnyType>>(), Arg.Any<Arg.AnyType>()))
           .Do(c => c.InvokeGenericConfigureScopeMethod(scope));
    }

    public static void InvokeGenericConfigureScopeMethod(
        this CallInfo c,
        Scope scope)
    {
        // If we use Arg.AnyType to look up the arguments, NSubstitute is unable to find
        // them. So as a workaround, we get the arguments directly and use reflection to
        // invoke the method.
        var action = c[0];
        var arg = c[1];
        action.GetType().GetMethod("Invoke")!.Invoke(action, [scope, arg]);
    }
}
