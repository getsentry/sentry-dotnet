namespace Sentry.Tests.Internals;

[UsesVerify]
public class MainExceptionProcessorTests
{
    private class Fixture
    {
        public ISentryStackTraceFactory SentryStackTraceFactory { get; set; } = Substitute.For<ISentryStackTraceFactory>();
        public SentryOptions SentryOptions { get; set; } = new();
        public MainExceptionProcessor GetSut() => new(SentryOptions, () => SentryStackTraceFactory);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Process_ExceptionAndEventWithoutExtra_ExtraIsEmpty()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        sut.Process(new Exception(), evt);

        Assert.Empty(evt.Extra);
    }

    [Fact]
    public void Process_ExceptionsWithoutData_ExtraIsEmpty()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        sut.Process(new Exception("ex", new Exception()), evt);

        Assert.Empty(evt.Extra);
    }

    [Fact]
    public void Process_EventAndExceptionHaveExtra_DataCombined()
    {
        const string expectedKey = "extra";
        const int expectedValue = 1;

        var sut = _fixture.GetSut();

        var evt = new SentryEvent();
        evt.SetExtra(expectedKey, expectedValue);

        var ex = new Exception();
        ex.Data.Add("other extra", 2);

        sut.Process(ex, evt);

        Assert.Equal(2, evt.Extra.Count);
        Assert.Contains(evt.Extra, p => p.Key == expectedKey && (int)p.Value == expectedValue);
    }

    [Fact]
    public void Process_EventHasNoExtrasExceptionDoes_DataInEvent()
    {
        const string expectedKey = "extra";
        const int expectedValue = 1;

        var sut = _fixture.GetSut();

        var evt = new SentryEvent();

        var ex = new Exception();
        ex.Data.Add(expectedKey, expectedValue);

        sut.Process(ex, evt);

        var actual = Assert.Single(evt.Extra);

        Assert.Equal($"Exception[0][{expectedKey}]", actual.Key);
        Assert.Equal(expectedValue, (int)actual.Value!);
    }

    [Fact]
    public void Process_EventHasExtrasExceptionDoesnt_NotModified()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        evt.SetExtra("extra", 1);
        var expected = evt.Extra;

        var ex = new Exception();

        sut.Process(ex, evt);

        Assert.Same(expected, evt.Extra);
    }

    [Fact]
    public void Process_TwoExceptionsWithData_DataOnEventExtra()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();

        var first = new Exception();
        var firstValue = new object();
        first.Data.Add("first", firstValue);
        var second = new Exception("second", first);
        var secondValue = new object();
        second.Data.Add("second", secondValue);

        sut.Process(second, evt);

        Assert.Equal(2, evt.Extra.Count);
        Assert.Contains(evt.Extra, e => e.Key == "Exception[0][first]" && e.Value == firstValue);
        Assert.Contains(evt.Extra, e => e.Key == "Exception[1][second]" && e.Value == secondValue);
    }

    [Fact]
    public void Process_ExceptionWithout_Handled()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        var exp = new Exception();

        sut.Process(exp, evt);

        _ = Assert.Single(evt.SentryExceptions!.Where(p => p.Mechanism!.Handled == null));
    }

    [Fact]
    public void Process_ExceptionWith_HandledTrue()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        var exp = new Exception();

        exp.Data.Add(Mechanism.HandledKey, true);
        exp.Data.Add(Mechanism.MechanismKey, "Process_ExceptionWith_HandledTrue");

        sut.Process(exp, evt);

        _ = Assert.Single(evt.SentryExceptions!.Where(p => p.Mechanism!.Handled == true));
    }

    [Fact]
    public void CreateSentryException_DataHasObjectAsKey_ItemIgnored()
    {
        var sut = _fixture.GetSut();
        var ex = new Exception
        {
            Data = { [new object()] = new object() }
        };

        var actual = sut.CreateSentryException(ex);

        Assert.Empty(actual.Single().Data);
    }

    [Fact]
    [Trait("Category", "Verify")]
    public Task CreateSentryException_Aggregate()
    {
        var sut = _fixture.GetSut();
        var aggregateException = BuildAggregateException();

        var sentryException = sut.CreateSentryException(aggregateException);

        return Verify(sentryException);
    }

    [Fact]
    [Trait("Category", "Verify")]
    public Task CreateSentryException_Aggregate_Keep()
    {
        _fixture.SentryOptions.KeepAggregateException = true;
        var sut = _fixture.GetSut();
        var aggregateException = BuildAggregateException();

        var sentryException = sut.CreateSentryException(aggregateException);

        return Verify(sentryException)
            .ScrubLines(x => x.Contains("One or more errors occurred"));
    }

    private static AggregateException BuildAggregateException()
    {
        return new AggregateException(
            new Exception("Inner message1"),
            new Exception("Inner message2"));
    }

    [Fact]
    public void Process_HasTagsOnExceptionData_TagsSet()
    {
        //Assert
        var sut = _fixture.GetSut();
        var expectedTag1 = new KeyValuePair<string, string>("Tag1", "1234");
        var expectedTag2 = new KeyValuePair<string, string>("Tag2", "4321");

        var ex = new Exception();
        var evt = new SentryEvent();

        //Act
        ex.AddSentryTag(expectedTag1.Key, expectedTag1.Value);
        ex.AddSentryTag(expectedTag2.Key, expectedTag2.Value);
        sut.Process(ex, evt);

        //Assert
        Assert.Single(evt.Tags, expectedTag1);
        Assert.Single(evt.Tags, expectedTag2);
    }

    [Fact]
    public void Process_HasInvalidExceptionTagsValue_TagsSetWithInvalidValue()
    {
        //Assert
        var sut = _fixture.GetSut();
        var InvalidTag = new KeyValuePair<string, int>("Tag1", 1234);
        var InvalidTag2 = new KeyValuePair<string, int?>("Tag2", null);
        var InvalidTag3 = new KeyValuePair<string, string>("", "abcd");

        var expectedTag = new KeyValuePair<string, object>("Exception[0][sentry:tag:Tag1]", 1234);
        var expectedTag2 = new KeyValuePair<string, object>("Exception[0][sentry:tag:Tag2]", null);
        var expectedTag3 = new KeyValuePair<string, object>("Exception[0][sentry:tag:]", "abcd");

        var ex = new Exception();
        var evt = new SentryEvent();

        //Act
        ex.Data.Add($"{MainExceptionProcessor.ExceptionDataTagKey}{InvalidTag.Key}", InvalidTag.Value);
        ex.Data.Add($"{MainExceptionProcessor.ExceptionDataTagKey}{InvalidTag2.Key}", InvalidTag2.Value);
        ex.AddSentryTag(InvalidTag3.Key, InvalidTag3.Value);

        sut.Process(ex, evt);

        //Assert
        Assert.Single(evt.Extra, expectedTag);
        Assert.Single(evt.Extra, expectedTag2);
        Assert.Single(evt.Extra, expectedTag3);
    }

    [Fact]
    public void Process_HasContextOnExceptionData_ContextSet()
    {
        //Assert
        var sut = _fixture.GetSut();
        var ex = new Exception();
        var evt = new SentryEvent();
        var expectedContext1 = new KeyValuePair<string, Dictionary<string, object>>("Context 1",
            new Dictionary<string, object>
            {
                { "Data1", new { a = 1, b = 2, c = "12345"} },
                { "Data2", "Something broke." }
            });
        var expectedContext2 = new KeyValuePair<string, Dictionary<string, object>>("Context 2",
            new Dictionary<string, object>
            {
                { "Data1", new { c = 1, d = 2, e = "12345"} },
                { "Data2", "Something broke again." }
            });

        //Act
        ex.AddSentryContext(expectedContext1.Key, expectedContext1.Value);
        ex.AddSentryContext(expectedContext2.Key, expectedContext2.Value);
        sut.Process(ex, evt);

        //Assert
        Assert.Equal(evt.Contexts[expectedContext1.Key], expectedContext1.Value);
        Assert.Equal(evt.Contexts[expectedContext2.Key], expectedContext2.Value);
    }

    [Fact]
    public void Process_HasContextOnExceptionDataWithNullValue_ContextSetWithInvalidValue()
    {
        //Assert
        var sut = _fixture.GetSut();
        var ex = new Exception();
        var evt = new SentryEvent();
        var invalidContext = new KeyValuePair<string, Dictionary<string, object>>("Context 1", null);
        var expectedContext = new KeyValuePair<string, object>("Exception[0][sentry:context:Context 1]", null);

        var invalidContext2 = new KeyValuePair<string, Dictionary<string, object>>("",
            new Dictionary<string, object>
            {
                { "Data3", new { c = 1, d = 2, e = "12345"} },
                { "Data4", "Something broke again." }
            });
        var expectedContext2 = new KeyValuePair<string, object>("Exception[0][sentry:context:]", invalidContext2.Value);

        //Act
        ex.AddSentryContext(invalidContext.Key, invalidContext.Value);
        ex.AddSentryContext(invalidContext2.Key, invalidContext2.Value);
        sut.Process(ex, evt);

        //Assert
        Assert.Single(evt.Extra, expectedContext);
        Assert.Single(evt.Extra, expectedContext2);
    }

    [Fact]
    public void Process_HasUnsupportedExceptionValue_ValueSetAsExtra()
    {
        //Assert
        var sut = _fixture.GetSut();
        var InvalidData = new KeyValuePair<string, string>("sentry:attachment:filename", "./path");
        var InvalidData2 = new KeyValuePair<string, int?>("sentry:unsupported:value", null);

        var expectedData = new KeyValuePair<string, object>($"Exception[0][{InvalidData.Key}]", InvalidData.Value);
        var expectedData2 = new KeyValuePair<string, object>($"Exception[0][{InvalidData2.Key}]", InvalidData2.Value);

        var ex = new Exception();
        var evt = new SentryEvent();

        //Act
        ex.Data.Add(InvalidData.Key, InvalidData.Value);
        ex.Data.Add(InvalidData2.Key, InvalidData2.Value);
        sut.Process(ex, evt);

        //Assert
        Assert.Single(evt.Extra, expectedData);
        Assert.Single(evt.Extra, expectedData2);
    }

    // TODO: Test when the approach for parsing is finalized
}
