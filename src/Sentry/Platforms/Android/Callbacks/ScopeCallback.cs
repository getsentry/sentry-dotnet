namespace Sentry.Android.Callbacks;

internal class ScopeCallback : JavaObject, JavaSdk.IScopeCallback
{
    private readonly Action<JavaSdk.IScope> _configureScope;

    public ScopeCallback(Action<JavaSdk.IScope> configureScope) =>
        _configureScope = configureScope;

    public void Run(JavaSdk.IScope scope) => _configureScope(scope);
}
