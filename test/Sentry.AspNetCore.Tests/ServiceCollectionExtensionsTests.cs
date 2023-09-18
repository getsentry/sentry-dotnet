using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore.Tests;

public class ServiceCollectionExtensionsTests
{
    private IServiceCollection _sut = Substitute.For<IServiceCollection>();

    [Fact]
    public void AddSentry_NoPreviousRegistration_RegistersHub()
    {
        _ = _sut.AddSentry();
        _sut.Received().Add(Arg.Is<ServiceDescriptor>(d => d.ServiceType == typeof(IHub)));
    }

    [Fact]
    public void AddSentry_NoPreviousRegistration_RegistersClient()
    {
        _ = _sut.AddSentry();
        _sut.Received().Add(Arg.Is<ServiceDescriptor>(d => d.ServiceType == typeof(ISentryClient)));
    }

    [Fact]
    public void AddSentry_PreviousRegistration_DoesNotRegistersHub()
    {
        var hub = Substitute.For<IHub>();
        _sut = new ServiceCollection();
        _ = _sut.AddSingleton(hub);

        _ = _sut.AddSentry();

        Assert.Same(hub, _sut.Single(s => s.ServiceType == typeof(IHub)).ImplementationInstance);
    }

    [Fact]
    public void AddSentry_PreviousRegistration_DoesNotRegistersClient()
    {
        var hub = Substitute.For<ISentryClient>();
        _sut = new ServiceCollection();
        _ = _sut.AddSingleton(hub);

        _ = _sut.AddSentry();

        Assert.Same(hub, _sut.Single(s => s.ServiceType == typeof(ISentryClient)).ImplementationInstance);
    }

    [Fact]
    public void AddSentry_PayloadExtractors_Registered()
    {
        _ = _sut.AddSentry();
        _sut.Received().Add(Arg.Is<ServiceDescriptor>(d => d.ServiceType == typeof(IRequestPayloadExtractor)
                                                           && d.ImplementationType == typeof(FormRequestPayloadExtractor)));

        _sut.Received().Add(Arg.Is<ServiceDescriptor>(d => d.ServiceType == typeof(IRequestPayloadExtractor)
                                                           && d.ImplementationType == typeof(DefaultRequestPayloadExtractor)));
    }

    [Fact]
    public void AddSentry_DefaultRequestPayloadExtractor_LastRegistration()
    {
        var hub = Substitute.For<ISentryClient>();
        _sut = new ServiceCollection();
        _ = _sut.AddSingleton(hub);

        _ = _sut.AddSentry();

        var last = _sut.Last(d => d.ServiceType == typeof(IRequestPayloadExtractor));
        Assert.Same(typeof(DefaultRequestPayloadExtractor), last.ImplementationType);
    }

#pragma warning disable CS0618
    [Fact]
    public void AddSentry_DefaultUserFactory_Registered()
    {
        _ = _sut.AddSentry();
        _sut.Received().Add(Arg.Is<ServiceDescriptor>(d => d.ServiceType == typeof(IUserFactory)
                                                           && d.ImplementationType == typeof(DefaultUserFactory)));
    }
#pragma warning restore CS0618
}
