using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Internals
{
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
            var ex = new Exception();
            ex.Data[new object()] = new object();

            var actual = sut.CreateSentryException(ex);

            Assert.Empty(actual.Single().Data);
        }

        [Fact]
        public void CreateSentryException_HasTagsOnExceptionData_TagsSetted()
        {
            //Assert
            var sut = _fixture.GetSut();
            var ex = new Exception();
            var evt = new SentryEvent();
            var tag1 = new KeyValuePair<string, string>("Tag1", "1234");
            var tag2 = new KeyValuePair<string, string>("Tag2", "4321");
            ex.Data.Add(MainExceptionProcessor.ExceptionDataTagKey + tag1.Key, tag1.Value);
            ex.Data.Add(MainExceptionProcessor.ExceptionDataTagKey + tag2.Key, tag2.Value);

            //Act
            sut.Process(ex, evt);

            //Assert
            Assert.Single(evt.Tags, tag1);
            Assert.Single(evt.Tags, tag2);
        }

        [Fact]
        public void CreateSentryException_HasInvalidTagsOnExceptionData_TagsAsExtra()
        {
            //Assert
            var sut = _fixture.GetSut();
            var ex = new Exception();
            var evt = new SentryEvent();

            var tag1 = new KeyValuePair<string, object>(MainExceptionProcessor.ExceptionDataTagKey + "Tag1", new { a = 1, b = 2 });
            var expectedTag1Extra = new KeyValuePair<string, object>($"Exception[0][{tag1.Key}]", tag1.Value);

            var tag2 = new KeyValuePair<string, string>(MainExceptionProcessor.ExceptionDataTagKey, "4321");
            var expectedTag2Extra = new KeyValuePair<string, object>($"Exception[0][{tag2.Key}]", tag2.Value);

            ex.Data.Add(tag1.Key, tag1.Value);
            ex.Data.Add(tag2.Key, tag2.Value);

            //Act
            sut.Process(ex, evt);

            //Assert
            Assert.Empty(evt.Tags);
            Assert.Single(evt.Extra, expectedTag1Extra);
            Assert.Single(evt.Extra, expectedTag2Extra);
        }

        [Fact]
        public void CreateSentryException_HasContextOnExceptionData_ContextSetted()
        {
            //Assert
            var sut = _fixture.GetSut();
            var ex = new Exception();
            var evt = new SentryEvent();
            var context1 = new KeyValuePair<string, Dictionary<string, object>>("Context 1",
                new Dictionary<string, object>
                {
                    { "Data1", new { a = 1, b = 2, c = "12345"} },
                    { "Data2", "Something broke." }
                });
            var context2 = new KeyValuePair<string, Dictionary<string, object>>("Context 2",
                new Dictionary<string, object>
                {
                    { "Data1", new { c = 1, d = 2, e = "12345"} },
                    { "Data2", "Something broke again." }
                });
            ex.Data.Add(MainExceptionProcessor.ExceptionDataContextKey + context1.Key, context1.Value);
            ex.Data.Add(MainExceptionProcessor.ExceptionDataContextKey + context2.Key, context2.Value);

            //Act
            sut.Process(ex, evt);

            //Assert
            Assert.Equal(evt.Contexts[context1.Key], context1.Value);
            Assert.Equal(evt.Contexts[context2.Key], context2.Value);
        }

        [Fact]
        public void CreateSentryException_HasInvalidContextOnExceptionData_ContextAsExtra()
        {
            //Assert
            var sut = _fixture.GetSut();
            var ex = new Exception();
            var evt = new SentryEvent();
            var context1 = new KeyValuePair<string, Dictionary<string, object>>("_",
                new Dictionary<string, object>
                {
                    { "Data1", new { a = 1, b = 2, c = "12345"} },
                    { "Data2", "Something broke." }
                });
            var expectedContentExtra = new KeyValuePair<string, object>($"Exception[0][{MainExceptionProcessor.ExceptionDataContextKey}]", context1.Value);
            ex.Data.Add(MainExceptionProcessor.ExceptionDataContextKey, context1.Value);

            //Act
            sut.Process(ex, evt);

            //Assert
            Assert.Empty(evt.Contexts);
            Assert.Single(evt.Extra, expectedContentExtra);
        }

        // TODO: Test when the approach for parsing is finalized
    }
}
