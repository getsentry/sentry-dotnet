using System.Text.Json;
using Sentry.Testing;

namespace Sentry.Tests.Protocol;

public class SentryIdTests
{
    [Fact]
    public void ToString_Equal_GuidToStringN()
    {
        var expected = Guid.NewGuid();
        SentryId actual = expected;
        Assert.Equal(expected.ToString("N"), actual.ToString());
    }

    [Fact]
    public void Implicit_ToGuid()
    {
        var expected = SentryId.Create();
        Guid actual = expected;
        Assert.Equal(expected.ToString(), actual.ToString("N"));
    }
    [Fact]
    public void Construct_guid_empty()
    {
#if DEBUG
        var exception = Assert.ThrowsAny<Exception>(()=>new SentryId(Guid.Empty));
        Assert.StartsWith("Dont use this API with Guid.Empty. Instead use SentryId.Empty", exception.Message);
#else
        var id = new SentryId(Guid.Empty);
        Assert.Equal(SentryId.Empty, id);
#endif
    }

    [Fact]
    public void ToGuid_empty()
    {
#if DEBUG
        var exception = Assert.ThrowsAny<Exception>(() => (Guid)SentryId.Empty);
        Assert.StartsWith("Dont use this API with Guid.Empty. Instead use SentryId.Empty", exception.Message);
#else
        Assert.Equal(Guid.Empty, (Guid)SentryId.Empty);
#endif
    }

    [Fact]
    public void FromGuid_empty()
    {
#if DEBUG
        var exception = Assert.ThrowsAny<Exception>(() => (SentryId)Guid.Empty);
        Assert.StartsWith("Dont use this API with Guid.Empty. Instead use SentryId.Empty", exception.Message);
#else
        Assert.Equal(SentryId.Empty, (SentryId)Guid.Empty);
#endif
    }

    [Fact]
    public void GetHashCode_empty()
    {
#if DEBUG
        var exception = Assert.ThrowsAny<Exception>(()=>SentryId.Empty.GetHashCode());
        Assert.StartsWith("Dont use this API with Guid.Empty. Instead use SentryId.Empty", exception.Message);
#else
        var hash = SentryId.Empty.GetHashCode();
        Assert.Equal(Guid.Empty.GetHashCode(), hash);
#endif
    }

    [Fact]
    public void Write_empty()
    {
        var logger = new InMemoryDiagnosticLogger();
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
#if DEBUG
        var exception = Assert.ThrowsAny<Exception>(()=>SentryId.Empty.WriteTo(writer, logger));
        Assert.StartsWith("Dont use this API with Guid.Empty. Instead use SentryId.Empty", exception.Message);
#else
        SentryId.Empty.WriteTo(writer, logger);
        writer.Flush();
        Assert.Equal("\"00000000000000000000000000000000\"", Encoding.UTF8.GetString(stream.ToArray()));
#endif
        Assert.Equal("WriteTo should not be called on SentryId.Empty", logger.Entries.Single().Message);
    }

    [Fact]
    public void Empty_Equal_GuidEmpty()
    {
        Assert.Equal(SentryId.Empty.ToString(), Guid.Empty.ToString("N"));
    }
}
