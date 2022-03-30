using System.Net;
using System.Net.Http;
using System.Text.Json;
using Sentry.Http;
using Sentry.Testing;

namespace Sentry.Tests.Internals.Http;

public class HttpContentReaderTests
{
    private const string TestString = "foo";
    private const string TestJson = "{\"foo\":\"bar\"}";

    [Fact]
    public void ReadString_Succeeds()
    {
        using var stream = TestString.ToMemoryStream();
        using var content = new StreamContent(stream);

        var reader = new HttpContentReader();
        var result = reader.ReadString(content);

        Assert.Equal(TestString, result);
    }

    [Fact]
    public async Task ReadStringAsync_Succeeds()
    {
        var stream = TestString.ToMemoryStream();
#if NETCOREAPP3_0_OR_GREATER
        await using (stream)
#else
        using (stream)
#endif
        {
            using var content = new StreamContent(stream);

            var reader = new HttpContentReader();
            var result = await reader.ReadStringAsync(content, default);

            Assert.Equal(TestString, result);
        }
    }

    [Fact]
    public void ReadJson_Succeeds()
    {
        using var stream = TestJson.ToMemoryStream();
        using var content = new StreamContent(stream);

        var reader = new HttpContentReader();
        var result = reader.ReadJson(content);

        Assert.Equal(TestJson, JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task ReadJsonAsync_Succeeds()
    {
        var stream = TestJson.ToMemoryStream();
#if NETCOREAPP3_0_OR_GREATER
        await using (stream)
#else
        using (stream)
#endif
        {
            using var content = new StreamContent(stream);

            var reader = new HttpContentReader();
            var result = await reader.ReadJsonAsync(content, default);

            Assert.Equal(TestJson, JsonSerializer.Serialize(result));
        }
    }

    [Fact]
    public void ReadString_CanBeOverridden()
    {
        using var content = new FakeContent<string>(TestString);
        var reader = new TestHttpContentReader();

        var result = reader.ReadString(content);

        Assert.Equal(TestString, result);
    }

    [Fact]
    public async Task ReadStringAsync_CanBeOverridden()
    {
        using var content = new FakeContent<string>(TestString);
        var reader = new TestHttpContentReader();

        var result = await reader.ReadStringAsync(content, default);

        Assert.Equal(TestString, result);
    }

    [Fact]
    public void ReadJson_CanBeOverridden()
    {
        var testJsonElement = JsonDocument.Parse(TestJson).RootElement;
        using var content = new FakeContent<JsonElement>(testJsonElement);
        var reader = new TestHttpContentReader();

        var result = reader.ReadJson(content);

        Assert.Equal(testJsonElement, result);
    }

    [Fact]
    public async Task ReadJsonAsync_CanBeOverridden()
    {
        var testJsonElement = JsonDocument.Parse(TestJson).RootElement;
        using var content = new FakeContent<JsonElement>(testJsonElement);
        var reader = new TestHttpContentReader();

        var result = await reader.ReadJsonAsync(content, default);

        Assert.Equal(testJsonElement, result);
    }

    private class FakeContent<TValue> : HttpContent
    {
        public FakeContent(TValue value)
        {
            Value = value;
        }

        public TValue Value { get; }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            throw new NotImplementedException();
        }

        protected override bool TryComputeLength(out long length)
        {
            throw new NotImplementedException();
        }
    }

    private class TestHttpContentReader : HttpContentReader
    {
        protected internal override string ReadString(HttpContent content)
        {
            return ((FakeContent<string>)content).Value;
        }

        protected internal override Task<string> ReadStringAsync(HttpContent content, CancellationToken cancellationToken)
        {
            return Task.FromResult(((FakeContent<string>)content).Value);
        }

        protected internal override JsonElement ReadJson(HttpContent content)
        {
            return ((FakeContent<JsonElement>)content).Value;
        }

        protected internal override Task<JsonElement> ReadJsonAsync(HttpContent content, CancellationToken cancellationToken)
        {
            return Task.FromResult(((FakeContent<JsonElement>)content).Value);
        }
    }

}
