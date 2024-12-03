namespace Sentry.Tests.Internals;

public partial class MainExceptionProcessorTests
{
    private class Fixture
    {
        public ISentryStackTraceFactory SentryStackTraceFactory { get; set; } = Substitute.For<ISentryStackTraceFactory>();
        public SentryOptions SentryOptions { get; set; } = new();
        public MainExceptionProcessor GetSut() => new(SentryOptions, () => SentryStackTraceFactory);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Process_ExceptionsWithoutData_MechanismDataIsEmpty()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        var ex = GetHandledException();

        sut.Process(ex, evt);

        var sentryException = evt.SentryExceptions!.Single();
        Assert.Empty(sentryException.Mechanism!.Data);
    }

    [Fact]
    public void Process_ExceptionsWithData_MechanismDataIsPopulated()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        var ex = GetHandledException();
        ex.Data["foo"] = 123;
        ex.Data["bar"] = 456;

        sut.Process(ex, evt);

        var sentryException = evt.SentryExceptions!.Single();
        var data = sentryException.Mechanism!.Data;

        Assert.Contains(data, pair => pair.Key == "foo" && pair.Value.Equals(123));
        Assert.Contains(data, pair => pair.Key == "bar" && pair.Value.Equals(456));
    }

    [Fact]
    public void Process_ExceptionWithout_Handled()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        var exp = new Exception();

        sut.Process(exp, evt);

        Assert.NotNull(evt.SentryExceptions);
        Assert.Single(evt.SentryExceptions, p => p.Mechanism?.Handled == null);
    }

    [Fact]
    public void Process_ExceptionWith_HandledFalse()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        var exp = new Exception();

        exp.Data.Add(Mechanism.HandledKey, false);

        sut.Process(exp, evt);

        Assert.NotNull(evt.SentryExceptions);
        Assert.Single(evt.SentryExceptions, p => p.Mechanism?.Handled == false);
    }

    [Fact]
    public void Process_ExceptionWith_HandledTrue()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        var exp = new Exception();

        exp.Data.Add(Mechanism.HandledKey, true);

        sut.Process(exp, evt);

        Assert.Single(evt.SentryExceptions, p => p.Mechanism?.Handled == true);
    }

    [Fact]
    public void Process_ExceptionWith_HandledTrue_WhenCaught()
    {
        var sut = _fixture.GetSut();
        var evt = new SentryEvent();
        var exp = GetHandledException();

        sut.Process(exp, evt);

        Assert.NotNull(evt.SentryExceptions);
        Assert.Single(evt.SentryExceptions, p => p.Mechanism?.Handled == true);
    }

    [Fact]
    public void CreateSentryException_DataHasObjectAsKey_ItemIgnored()
    {
        var sut = _fixture.GetSut();
        var ex = new Exception
        {
            Data = { [new object()] = new object() }
        };

        var actual = sut.CreateSentryExceptions(ex).Single();

        Assert.Null(actual.Mechanism);
    }

    [Fact]
    public void Process_AggregateException()
    {
        var sut = _fixture.GetSut();
        _fixture.SentryStackTraceFactory = _fixture.SentryOptions.SentryStackTraceFactory;
        var evt = new SentryEvent();
        sut.Process(BuildAggregateException(), evt);

        var last = evt.SentryExceptions!.Last();
        // TODO: Create integration test to test this behaviour when publishing AOT apps
        // See https://github.com/getsentry/sentry-dotnet/issues/2772
        Assert.NotNull(last.Stacktrace);
        var mechanism = last.Mechanism;
        Assert.NotNull(mechanism);
        Assert.False(mechanism.Handled);
        Assert.NotNull(mechanism.Type);
        Assert.NotEmpty(mechanism.Data);
    }

    private static AggregateException BuildAggregateException()
    {
        try
        {
            // Throwing will put a stack trace on the exception
            throw new AggregateException("One or more errors occurred.",
                new Exception("Inner message1"),
                new Exception("Inner message2"));
        }
        catch (AggregateException exception)
        {
            // Add extra data to test fully
            exception.Data[Mechanism.HandledKey] = false;
            exception.Data[Mechanism.MechanismKey] = "AppDomain.UnhandledException";
            exception.Data["foo"] = "bar";
            return exception;
        }
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
        var invalidTag1 = new KeyValuePair<string, int>("Tag1", 1234);
        var invalidTag2 = new KeyValuePair<string, int?>("Tag2", null);
        var invalidTag3 = new KeyValuePair<string, string>("", "abcd");

        var expectedTag1 = new KeyValuePair<string, object>("Exception[0][sentry:tag:Tag1]", 1234);
        var expectedTag2 = new KeyValuePair<string, object>("Exception[0][sentry:tag:Tag2]", null);
        var expectedTag3 = new KeyValuePair<string, object>("Exception[0][sentry:tag:]", "abcd");

        var ex = new Exception();
        var evt = new SentryEvent();

        //Act
        ex.Data.Add($"{MainExceptionProcessor.ExceptionDataTagKey}{invalidTag1.Key}", invalidTag1.Value);
        ex.Data.Add($"{MainExceptionProcessor.ExceptionDataTagKey}{invalidTag2.Key}", invalidTag2.Value);
        ex.AddSentryTag(invalidTag3.Key, invalidTag3.Value);

        sut.Process(ex, evt);

        //Assert
        Assert.Single(evt.Extra, expectedTag1);
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
        var invalidData1 = new KeyValuePair<string, string>("sentry:attachment:filename", "./path");
        var invalidData2 = new KeyValuePair<string, int?>("sentry:unsupported:value", null);

        var expectedData = new KeyValuePair<string, object>($"Exception[0][{invalidData1.Key}]", invalidData1.Value);
        var expectedData2 = new KeyValuePair<string, object>($"Exception[0][{invalidData2.Key}]", invalidData2.Value);

        var ex = new Exception();
        var evt = new SentryEvent();

        //Act
        ex.Data.Add(invalidData1.Key, invalidData1.Value);
        ex.Data.Add(invalidData2.Key, invalidData2.Value);
        sut.Process(ex, evt);

        //Assert
        Assert.Single(evt.Extra, expectedData);
        Assert.Single(evt.Extra, expectedData2);
    }

    private Exception GetHandledException()
    {
        try
        {
            throw new Exception();
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
