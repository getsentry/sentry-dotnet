using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore.Tests;

public class LifetimeSpecificServiceResolversTests
{
    private interface IMyInterface {}
    private class ServiceA : IMyInterface {}
    private class ServiceB: IMyInterface {}
    private class ServiceC : IMyInterface {}
    private class ServiceD : IMyInterface {}

    private class ServiceE(Foo foo) : IMyInterface
    {
        public void Foo() => foo.Bar();
    }

    private class Foo
    {
        public void Bar() => Console.WriteLine("bar");
    }

    [Fact]
    public void ShouldResolveSingletonDependencies()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Register services with various lifetimes
        serviceCollection.AddSingleton<IMyInterface>(new ServiceA());
        serviceCollection.AddTransient<IMyInterface, ServiceB>();
        serviceCollection.AddSingleton<IMyInterface>(_ => new ServiceC());

        serviceCollection.AddSingleton(new LifetimeServiceResolver(serviceCollection));

        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act
        var instances = serviceProvider
            .GetServices<IMyInterface>([ServiceLifetime.Singleton])
            .ToArray();

        // Assert
        using (new AssertionScope())
        {
            instances.Length.Should().Be(2);
            instances.Should().Contain(x => x.GetType() == typeof(ServiceA));
            instances.Should().Contain(x => x.GetType() == typeof(ServiceC));
        }
    }

    [Fact]
    public void ShouldResolveTransientDependencies()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Register services with various lifetimes
        serviceCollection.AddTransient<IMyInterface, ServiceA>();
        serviceCollection.AddScoped<IMyInterface>(_ => new ServiceB());
        serviceCollection.AddTransient<IMyInterface>(_ => new ServiceC());
        serviceCollection.AddSingleton<IMyInterface>(new ServiceD());

        serviceCollection.AddSingleton(new LifetimeServiceResolver(serviceCollection));

        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act
        var instances = serviceProvider
            .GetServices<IMyInterface>([ServiceLifetime.Transient])
            .ToArray();

        // Assert
        using (new AssertionScope())
        {
            instances.Length.Should().Be(2);
            instances.Should().Contain(x => x.GetType() == typeof(ServiceA));
            instances.Should().Contain(x => x.GetType() == typeof(ServiceC));
        }
    }

    [Fact]
    public void ShouldResolveScopedDependencies()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Register services with various lifetimes
        serviceCollection.AddScoped<Foo>();
        serviceCollection.AddScoped<IMyInterface, ServiceE>();
        serviceCollection.AddScoped<IMyInterface>(_ => new ServiceB());
        serviceCollection.AddTransient<IMyInterface>(_ => new ServiceC());
        serviceCollection.AddSingleton<IMyInterface>(new ServiceD());

        serviceCollection.AddSingleton(new LifetimeServiceResolver(serviceCollection));

        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act
        var instances = serviceProvider
            .GetServices<IMyInterface>([ServiceLifetime.Scoped])
            .ToArray();

        // Assert
        using (new AssertionScope())
        {
            instances.Length.Should().Be(2);
            instances.Should().Contain(x => x.GetType() == typeof(ServiceE));
            instances.Should().Contain(x => x.GetType() == typeof(ServiceB));
        }
    }
}
