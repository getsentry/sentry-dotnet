namespace Sentry.Extensions.Logging.Tests;

public class SentryLoggerFormatterTests
{
    private class Fixture
    {
        public SentryLoggerFormatter GetSut() => SentryLoggerFormatter.Instance;
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Log_class_destructured()
    {
        var sut = _fixture.GetSut();

        var parameter = new ParameterClass
        {
            BoolProperty = true,
            IntProperty = int.MaxValue,
            StringProperty = "some string"
        };

        IEnumerable<KeyValuePair<string, object>> state = new[]
        {
            new KeyValuePair<string, object>("@obj", parameter),
            new KeyValuePair<string, object>("{OriginalFormat}", "{@obj}")
        };

        var result = sut.Invoke(state);

        result.Should().Be(JsonSerializer.Serialize(parameter));
    }

    [Fact]
    public void Log_class_ToString()
    {
        var sut = _fixture.GetSut();

        var parameter = new ParameterClass
        {
            BoolProperty = true,
            IntProperty = int.MaxValue,
            StringProperty = "some string"
        };

        IEnumerable<KeyValuePair<string, object>> state = new[]
        {
            new KeyValuePair<string, object>("obj", parameter),
            new KeyValuePair<string, object>("{OriginalFormat}", "{obj}")
        };

        var result = sut.Invoke(state);

        // ReSharper disable StringLiteralTypo
        result.Should().Be("ssalCretemaraP");
        // ReSharper enable StringLiteralTypo
    }

    private class ParameterClass
    {
        public string StringProperty { get; set; }
        public int IntProperty { get; set; }
        public bool BoolProperty { get; set; }

        public override string ToString()
        {
            var charArray = nameof(ParameterClass).ToCharArray();
            Array.Reverse(charArray);

            return new string(charArray);
        }
    }
}
