namespace Sentry.Tests.Protocol;

public class RequestTests
{
    [Fact]
    public void Clone_CopyValues()
    {
        var sut = new SentryRequest
        {
            ApiTarget = "graphql",
            Url = "https://sentry.io",
            Method = "OPTIONS",
            Data = new object(),
            QueryString = "?query=string",
        };
        sut.Headers.Add("X-Test", "header");
        sut.Env.Add("SENTRY_ENV", "env");
        sut.Other.Add("other key", "other value");

        var clone = sut.Clone();

        Assert.Equal(sut.ApiTarget, clone.ApiTarget);
        Assert.Equal(sut.Url, clone.Url);
        Assert.Equal(sut.Method, clone.Method);
        Assert.Same(sut.Data, clone.Data);
        Assert.Equal(sut.QueryString, clone.QueryString);

        Assert.NotSame(sut.InternalHeaders, clone.InternalHeaders);
        Assert.NotSame(sut.InternalEnv, clone.InternalEnv);
        Assert.NotSame(sut.InternalOther, clone.InternalOther);

        Assert.Equal(sut.Headers.First().Key, clone.Headers.First().Key);
        Assert.Equal(sut.Headers.First().Value, clone.Headers.First().Value);

        Assert.Equal(sut.Env.First().Key, clone.Env.First().Key);
        Assert.Equal(sut.Env.First().Value, clone.Env.First().Value);

        Assert.Equal(sut.Other.First().Key, clone.Other.First().Key);
        Assert.Equal(sut.Other.First().Value, clone.Other.First().Value);
    }
}
