namespace Sentry.Tests.Protocol;

public class BaseScopeTests
{
    private readonly Scope _sut = new(new SentryOptions());

    [Fact]
    public void Fingerprint_ByDefault_ReturnsEmptyEnumerable()
    {
        Assert.Empty(_sut.Fingerprint);
    }

    [Fact]
    public void Tags_ByDefault_ReturnsEmpty()
    {
        Assert.Empty(_sut.Tags);
    }

    [Fact]
    public void Breadcrumbs_ByDefault_ReturnsEmpty()
    {
        Assert.Empty(_sut.Breadcrumbs);
    }

    [Fact]
    public void Sdk_ByDefault_ReturnsNotNull()
    {
        Assert.NotNull(_sut.Sdk);
    }

    [Fact]
    public void User_ByDefault_ReturnsNotNull()
    {
        Assert.NotNull(_sut.User);
    }

    [Fact]
    public void User_Settable()
    {
        var expected = new SentryUser();
        _sut.User = expected;
        Assert.Same(expected, _sut.User);
    }

    [Fact]
    public void Contexts_ByDefault_NotNull()
    {
        Assert.NotNull(_sut.Contexts);
    }

    [Fact]
    public void Contexts_Settable()
    {
        _sut.Contexts.App.Name = "Foo";

        var expected = new SentryContexts
        {
            App =
            {
                Name = "Bar"
            }
        };

        _sut.Contexts = expected;

        Assert.Equal(expected, _sut.Contexts);
        Assert.NotSame(expected, _sut.Contexts);
    }

    [Fact]
    public void Request_ByDefault_NotNull()
    {
        Assert.NotNull(_sut.Request);
    }

    [Fact]
    public void Request_Settable()
    {
        var expected = new SentryRequest();
        _sut.Request = expected;
        Assert.Same(expected, _sut.Request);
    }

    [Fact]
    public void Transaction_Settable()
    {
        var expected = "Transaction";
        _sut.TransactionName = expected;
        Assert.Same(expected, _sut.TransactionName);
    }

    [Fact]
    public void Release_Settable()
    {
        var expected = "Release";
        _sut.Release = expected;
        Assert.Same(expected, _sut.Release);
    }

    [Fact]
    public void Distribution_Settable()
    {
        var expected = "Distribution";
        _sut.Distribution = expected;
        Assert.Same(expected, _sut.Distribution);
    }

    [Fact]
    public void Environment_Settable()
    {
        var expected = "Environment";
        _sut.Environment = expected;
        Assert.Same(expected, _sut.Environment);
    }
}
