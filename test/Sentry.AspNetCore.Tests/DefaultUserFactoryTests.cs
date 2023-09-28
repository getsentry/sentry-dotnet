using Microsoft.AspNetCore.Http;

namespace Sentry.AspNetCore.Tests;

public class DefaultUserFactoryTests
{
    public HttpContext HttpContext { get; set; } = Substitute.For<HttpContext>();
    public IIdentity Identity { get; set; } = Substitute.For<IIdentity>();
    public ClaimsPrincipal User { get; set; } = Substitute.For<ClaimsPrincipal>();
    public ConnectionInfo ConnectionInfo { get; set; } = Substitute.For<ConnectionInfo>();
    public List<Claim> Claims { get; set; }

    public DefaultUserFactoryTests()
    {
        const string username = "test-user";
        _ = Identity.Name.Returns(username); // by default reads: ClaimTypes.Name
        Claims = new List<Claim>
        {
            new(ClaimTypes.Email, username +"@sentry.io"),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.NameIdentifier, "927391237"),
        };

        _ = ConnectionInfo.RemoteIpAddress.Returns(IPAddress.IPv6Loopback);

        _ = User.Identity.Returns(Identity);
        _ = HttpContext.User.Returns(User);
        _ = HttpContext.User.Claims.Returns(Claims);
        _ = HttpContext.Connection.Returns(ConnectionInfo);
    }

    private readonly DefaultUserFactory _sut = new();

    [Fact]
    public void Create_Fixture_CreatesCompleteUser()
    {
        var actual = _sut.Create(HttpContext);

        Assert.NotNull(actual);
        Assert.Equal(Claims.NameIdentifier(), actual.Id);
        Assert.Equal(Claims.Name(), actual.Username);
        Assert.Equal(Identity.Name, actual.Username);
        Assert.Equal(Claims.Email(), actual.Email);
        Assert.Equal(IPAddress.IPv6Loopback.ToString(), actual.IpAddress);
    }

    [Fact]
    public void Create_NoUser_Null()
    {
        _ = HttpContext.User.ReturnsNull();
        Assert.Null(_sut.Create(HttpContext));
    }

    [Fact]
    public void Create_NoClaimsNoIdentityNoIpAddress_Null()
    {
        _ = HttpContext.User.Identity.ReturnsNull();
        _ = HttpContext.User.Claims.Returns(Enumerable.Empty<Claim>());
        _ = HttpContext.Connection.ReturnsNull();
        Assert.Null(_sut.Create(HttpContext));
    }

    [Fact]
    public void Create_NoClaimsNoIdentity_IpAddress()
    {
        _ = HttpContext.User.Identity.ReturnsNull();
        _ = HttpContext.User.Claims.Returns(Enumerable.Empty<Claim>());
        var actual = _sut.Create(HttpContext);
        Assert.Equal(IPAddress.IPv6Loopback.ToString(), actual?.IpAddress);
    }

    [Fact]
    public void Create_NoClaims_UsernameFromIdentity()
    {
        _ = HttpContext.User.Claims.Returns(Enumerable.Empty<Claim>());
        var actual = _sut.Create(HttpContext);
        Assert.Equal(Identity.Name, actual.Username);
    }

    [Fact]
    public void Create_NoClaims_IpAddress()
    {
        _ = HttpContext.User.Claims.Returns(Enumerable.Empty<Claim>());
        var actual = _sut.Create(HttpContext);
        Assert.Equal(IPAddress.IPv6Loopback.ToString(), actual.IpAddress);
    }

    [Fact]
    public void Create_ContextAccessorNoClaims_IpAddress()
    {
        _ = HttpContext.User.Claims.Returns(Enumerable.Empty<Claim>());
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        contextAccessor.HttpContext.Returns(HttpContext);
        var actual = new DefaultUserFactory(contextAccessor).Create();
        Assert.Equal(IPAddress.IPv6Loopback.ToString(), actual?.IpAddress);
    }

    [Fact]
    public void Create_ClaimNameAndIdentityDontMatch_UsernameFromIdentity()
    {
        const string expected = "App configured to read it from a different claim";
        _ = User.Identity.Name.Returns(expected);
        var actual = _sut.Create(HttpContext);

        Assert.Equal(expected, actual.Username);
    }

    [Fact]
    public void Create_Id_FromClaims()
    {
        _ = Claims.RemoveAll(p => p.Type != ClaimTypes.NameIdentifier);
        var actual = _sut.Create(HttpContext);
        Assert.Equal(Claims.NameIdentifier(), actual.Id);
    }

    [Fact]
    public void Create_Username_FromClaims()
    {
        _ = Claims.RemoveAll(p => p.Type != ClaimTypes.Name);
        var actual = _sut.Create(HttpContext);
        Assert.Equal(Claims.Name(), actual.Username);
    }

    [Fact]
    public void Create_Username_FromIdentity()
    {
        _ = Claims.RemoveAll(p => p.Type != ClaimTypes.Name);
        var actual = _sut.Create(HttpContext);
        Assert.Equal(Identity.Name, actual.Username);
    }

    [Fact]
    public void Create_Email_FromClaims()
    {
        _ = Claims.RemoveAll(p => p.Type != ClaimTypes.Email);
        var actual = _sut.Create(HttpContext);
        Assert.Equal(Claims.Email(), actual.Email);
    }

    [Fact]
    public void Create_NoRemoteIpAddress_NoIpAvailable()
    {
        _ = ConnectionInfo.RemoteIpAddress.Returns(null as IPAddress);
        var actual = _sut.Create(HttpContext);
        Assert.Null(actual.IpAddress);
    }

    [Fact]
    public void Create_IpAddress_FromConnectionRemote()
    {
        var actual = _sut.Create(HttpContext);
        Assert.Equal(IPAddress.IPv6Loopback.ToString(), actual.IpAddress);
    }
}
