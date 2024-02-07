using OperatingSystem = Sentry.Protocol.OperatingSystem;

namespace Sentry.Tests.Protocol.Context;

public class ContextsTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public ContextsTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void SerializeObject_NoPropertyFilled_SerializesEmptyObject()
    {
        var sut = new SentryContexts();

        var actualString = sut.ToJsonString(_testOutputLogger);

        var actual = Json.Parse(actualString, SentryContexts.FromJson);
        actual.Should().BeEquivalentTo(sut);

        Assert.Equal("{}", actualString);
    }

    [Fact]
    public void SerializeObject_SingleUserDefinedKeyPropertySet_SerializeSingleProperty()
    {
        const string expectedKey = "server";
        var os = new OperatingSystem { Name = "Linux" };
        var sut = new SentryContexts
        {
            [expectedKey] = os
        };

        var actualString = sut.ToJsonString(_testOutputLogger);

        var actual = Json.Parse(actualString, SentryContexts.FromJson);
        actual.Should().BeEquivalentTo(sut);

        Assert.Equal("""{"server":{"type":"os","name":"Linux"}}""", actualString);
    }

    [Fact]
    public void SerializeObject_SingleDevicePropertySet_SerializeSingleProperty()
    {
        var sut = new SentryContexts
        {
            Device =
            {
                Architecture = "x86"
            }
        };

        var actualString = sut.ToJsonString(_testOutputLogger);

        var actual = Json.Parse(actualString, SentryContexts.FromJson);
        actual.Should().BeEquivalentTo(sut);

        Assert.Equal("""{"device":{"type":"device","arch":"x86"}}""", actualString);
    }

    [Fact]
    public void SerializeObject_SingleAppPropertySet_SerializeSingleProperty()
    {
        var sut = new SentryContexts
        {
            App =
            {
                Name = "My.App"
            }
        };

        var actualString = sut.ToJsonString(_testOutputLogger);

        var actual = Json.Parse(actualString, SentryContexts.FromJson);
        actual.Should().BeEquivalentTo(sut);

        Assert.Equal("""{"app":{"type":"app","app_name":"My.App"}}""", actualString);
    }

    [Fact]
    public void SerializeObject_SingleGpuPropertySet_SerializeSingleProperty()
    {
        var sut = new SentryContexts
        {
            Gpu =
            {
                Name = "My.Gpu"
            }
        };

        var actualString = sut.ToJsonString(_testOutputLogger);

        var actual = Json.Parse(actualString, SentryContexts.FromJson);
        actual.Should().BeEquivalentTo(sut);

        Assert.Equal("""{"gpu":{"type":"gpu","name":"My.Gpu"}}""", actualString);
    }

    [Fact]
    public void SerializeObject_SingleResponsePropertySet_SerializeSingleProperty()
    {
        var sut = new SentryContexts
        {
            Response =
            {
                StatusCode = 200
            }
        };

        var actualString = sut.ToJsonString(_testOutputLogger);

        var actual = Json.Parse(actualString, SentryContexts.FromJson);
        actual.Should().BeEquivalentTo(sut);

        Assert.Equal("""{"response":{"type":"response","status_code":200}}""", actualString);
    }

    [Fact]
    public void SerializeObject_SingleRuntimePropertySet_SerializeSingleProperty()
    {
        var sut = new SentryContexts
        {
            Runtime =
            {
                Version = "2.1.1.100"
            }
        };

        var actualString = sut.ToJsonString(_testOutputLogger);

        var actual = Json.Parse(actualString, SentryContexts.FromJson);
        actual.Should().BeEquivalentTo(sut);

        Assert.Equal("""{"runtime":{"type":"runtime","version":"2.1.1.100"}}""", actualString);
    }

    [Fact]
    public void SerializeObject_AnonymousObject_SerializedCorrectly()
    {
        // Arrange
        var contexts = new SentryContexts
        {
            ["foo"] = new { Bar = 42, Baz = "kek" }
        };

        // Act
        var json = contexts.ToJsonString(_testOutputLogger);
        var roundtrip = Json.Parse(json, SentryContexts.FromJson);

        // Assert
        json.Should().Be("""{"foo":{"Bar":42,"Baz":"kek"}}""");

        roundtrip["foo"].Should().BeEquivalentTo(new Dictionary<string, object>
        {
            ["Bar"] = 42,
            ["Baz"] = "kek"
        });
    }

    [Fact]
    public void SerializeObject_SortsContextKeys()
    {
        var sut = new SentryContexts
        {
            ["c"] = "3",
            ["a"] = "1",
            ["b"] = "2",
        };

        var actualString = sut.ToJsonString(_testOutputLogger);
        Assert.Equal("""{"a":"1","b":"2","c":"3"}""", actualString);
    }

    [Fact]
    public void SerializeObject_Null_Should_Be_Ignored()
    {
        // Arrange
        var contexts = new SentryContexts
        {
            ["key"] = null
        };

        // Act
        var json = contexts.ToJsonString(_testOutputLogger);
        var roundtrip = Json.Parse(json, SentryContexts.FromJson);

        // Assert
        json.Should().Be("{}");

        roundtrip.ContainsKey("key").Should().BeFalse();
    }

    [Fact]
    public void Ctor_SingleBrowserPropertySet_SerializeSingleProperty()
    {
        var sut = new SentryContexts
        {
            Browser =
            {
                Name = "Netscape 1"
            }
        };

        var actualString = sut.ToJsonString(_testOutputLogger);

        var actual = Json.Parse(actualString, SentryContexts.FromJson);
        actual.Should().BeEquivalentTo(sut);

        Assert.Equal("""{"browser":{"type":"browser","name":"Netscape 1"}}""", actualString);
    }

    [Fact]
    public void Ctor_SingleOperatingSystemPropertySet_SerializeSingleProperty()
    {
        var sut = new SentryContexts
        {
            OperatingSystem =
            {
                Name = "BeOS 1"
            }
        };

        var actualString = sut.ToJsonString(_testOutputLogger);

        var actual = Json.Parse(actualString, SentryContexts.FromJson);
        actual.Should().BeEquivalentTo(sut);

        Assert.Equal("""{"os":{"type":"os","name":"BeOS 1"}}""", actualString);
    }

    [Fact]
    public void Clone_CopyValues()
    {
        var sut = new SentryContexts
        {
            App =
            {
                Name = "name"
            }
        };
        const string expectedKey = "key";

        sut[expectedKey] = new object();

        var clone = sut.Clone();

        Assert.Equal(sut.App.Name, clone.App.Name);
        Assert.Same(sut[expectedKey], clone[expectedKey]);
    }
}
