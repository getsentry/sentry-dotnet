using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Sentry.AspNetCore.Grpc.Tests;

public class SentryBuilderExtensionsTests
{
    [Fact]
    public void AddGrpc_Sdk_IsGrpc()
    {
        var services = new ServiceCollection();
        var builder = Substitute.For<ISentryBuilder>();
        builder.Services.Returns(services);

        builder.AddGrpc();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SentryAspNetCoreOptions>>().Value;
        Assert.Equal("sentry.dotnet.aspnetcore.grpc", options.ScopeDefaults.Sdk.Name);
        Assert.Contains(options.ScopeDefaults.Sdk.Packages, p => p.Name == "nuget:Sentry.AspNetCore");
        Assert.Contains(options.ScopeDefaults.Sdk.Packages, p => p.Name == "nuget:Sentry.AspNetCore.Grpc");
    }
}
