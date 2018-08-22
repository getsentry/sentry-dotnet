using System;
using System.Diagnostics;
using System.Linq;
using Sentry;
using Sentry.Internal;
using Sentry.Protocol;
using Xunit;

// ReSharper disable once CheckNamespace
// Stack trace filters out Sentry frames by namespace
namespace Other.Tests.Internals
{
    public class SentryStackTraceFactoryTests
    {
        private class Fixture
        {
            public SentryOptions SentryOptions { get; set; } = new SentryOptions();
            public SentryStackTraceFactory GetSut() => new SentryStackTraceFactory(SentryOptions);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void Create_NoExceptionAndDefaultAttachStackTraceOption_NullResult()
        {
            var sut = _fixture.GetSut();

            Assert.Null(sut.Create());
        }

        [Fact]
        public void Create_NoExceptionAndAttachStackTraceOptionOn_CurrentStackTrace()
        {
            _fixture.SentryOptions.AttachStacktrace = true;
            var sut = _fixture.GetSut();

            var stackTrace = sut.Create();

            Assert.NotNull(stackTrace);
            Assert.Equal(nameof(Create_NoExceptionAndAttachStackTraceOptionOn_CurrentStackTrace), stackTrace.Frames.Last().Function);
            Assert.DoesNotContain(stackTrace.Frames, p => p.Function == nameof(SentryStackTraceFactory.CreateFrame));
        }

        [Fact]
        public void Create_NWithExceptionAndDefaultAttachStackTraceOption_NotNullResult()
        {
            var sut = _fixture.GetSut();

            Assert.NotNull(sut.Create(new Exception()));
        }

        [Fact]
        public void CreateSentryStackFrame_AppNamespace_InAppFrame()
        {
            var frame = new StackFrame();
            var sut = _fixture.GetSut();

            var actual = sut.CreateFrame(frame);

            Assert.True(actual.InApp);
        }

        [Fact]
        public void CreateSentryStackFrame_AppNamespaceExcluded_NotInAppFrame()
        {
            _fixture.SentryOptions.AddInAppExclude(GetType().Namespace);
            var sut = _fixture.GetSut();
            var frame = new StackFrame();

            var actual = sut.CreateFrame(frame);

            Assert.False(actual.InApp);
        }

        // https://github.com/getsentry/sentry-dotnet/issues/64
        [Fact]
        public void DemangleAnonymousFunction_NullFunction_ContinuesNull()
        {
            var stackFrame = new SentryStackFrame
            {
                Function = null
            };

            SentryStackTraceFactory.DemangleAnonymousFunction(stackFrame);
            Assert.Null(stackFrame.Function);
        }

        [Fact]
        public void DemangleAsyncFunctionName_NullModule_ContinuesNull()
        {
            var stackFrame = new SentryStackFrame
            {
                Module = null
            };

            SentryStackTraceFactory.DemangleAnonymousFunction(stackFrame);
            Assert.Null(stackFrame.Module);
        }
    }
}
