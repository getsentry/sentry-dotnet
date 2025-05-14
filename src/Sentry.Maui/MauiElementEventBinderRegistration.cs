namespace Sentry.Maui;

internal interface IMauiElementEventBinderRegistration
{
    void Register(IServiceCollection services);
}

internal class MauiElementEventBinderRegistration<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEventBinder> : IMauiElementEventBinderRegistration
    where TEventBinder : class, IMauiElementEventBinder
{
    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IMauiElementEventBinder, TEventBinder>();
    }
}
