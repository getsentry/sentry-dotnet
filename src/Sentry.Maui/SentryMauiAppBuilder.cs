namespace Sentry.Maui;

/// <summary>
/// A builder for configuring Sentry in a .NET MAUI application.
/// </summary>
public class SentryMauiAppBuilder(IServiceCollection services)
{
    /// <summary>
    /// Access the current service collection
    /// </summary>
    public IServiceCollection Services => services;

    /// <summary>
    /// Configures the application by adding a binding for a Maui element of the specified implementation type.
    /// </summary>
    /// <typeparam name="TEventBinder">The type of implementation for the Maui element binder to be added.</typeparam>
    /// <returns>The current instance of <see cref="SentryMauiAppBuilder"/> to allow method chaining.</returns>
    public SentryMauiAppBuilder AddMauiElementBinder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEventBinder>() where TEventBinder : class, IMauiElementEventBinder
    {
        Services.AddSingleton<IMauiElementEventBinder, TEventBinder>();
        return this;
    }
}
