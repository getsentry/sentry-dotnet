namespace Sentry.Maui;

/// <summary>
/// A builder for configuring Sentry in a .NET MAUI application.
/// </summary>
public class SentryMauiAppBuilder(IServiceCollection services)
{
    public IServiceCollection Services => services;

    /// <summary>
    /// Configures the application by adding a binding for a Maui element of the specified implementation type.
    /// </summary>
    /// <typeparam name="TImpl">The type of implementation for the Maui element binder to be added.</typeparam>
    /// <returns>The current instance of <see cref="SentryMauiAppBuilder"/> to allow method chaining.</returns>
    public SentryMauiAppBuilder AddMauiElementBinder<TImpl>() where TImpl : class, IMauiElementEventBinder
    {
        services.AddSingleton<IMauiElementEventBinder, TImpl>();
        return this;
    }
}
