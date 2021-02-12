using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Sentry;
using Sentry.Extensibility;
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
            public SentryOptions SentryOptions { get; set; } = new();
            public SentryStackTraceFactory GetSut() => new(SentryOptions);
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public void Create_NoExceptionAndDefaultAttachStackTraceOption_NullResult()
        {
            var sut = _fixture.GetSut();

            Assert.Null(sut.Create());
        }

        [Fact]
        public void Create_NoExceptionAndAttachStackTraceOptionOnWithOriginalMode_CurrentStackTrace()
        {
            _fixture.SentryOptions.AttachStacktrace = true;
            _fixture.SentryOptions.StackTraceMode = StackTraceMode.Original;
            var sut = _fixture.GetSut();

            var stackTrace = sut.Create();

            Assert.NotNull(stackTrace);

            Assert.Equal(
                nameof(Create_NoExceptionAndAttachStackTraceOptionOnWithOriginalMode_CurrentStackTrace),
                stackTrace.Frames.Last().Function
            );

            Assert.DoesNotContain(stackTrace.Frames, p =>
                p.Function?.StartsWith(
                    nameof(SentryStackTraceFactory.CreateFrame) + '(',
                    StringComparison.Ordinal
                ) == true
            );
        }

        [Fact]
        public void Create_NoExceptionAndAttachStackTraceOptionOnWithEnhancedMode_CurrentStackTrace()
        {
            _fixture.SentryOptions.AttachStacktrace = true;
            _fixture.SentryOptions.StackTraceMode = StackTraceMode.Enhanced;
            var sut = _fixture.GetSut();

            var stackTrace = sut.Create();

            Assert.NotNull(stackTrace);

            Assert.Equal(
                $"void " +
                $"{GetType().Name}" +
                $".{nameof(Create_NoExceptionAndAttachStackTraceOptionOnWithEnhancedMode_CurrentStackTrace)}" +
                "()",
                stackTrace.Frames.Last().Function
            );

            Assert.DoesNotContain(stackTrace.Frames, p =>
                p.Function?.StartsWith(
                    nameof(SentryStackTraceFactory.CreateFrame) + '(',
                    StringComparison.Ordinal
                ) == true
            );
        }

        [Fact]
        public void Create_WithExceptionAndDefaultAttachStackTraceOption_HasStackTrace()
        {
            var sut = _fixture.GetSut();

            Exception exception;
            try
            {
                Throw();
                void Throw() => throw null;
            }
            catch (Exception e) { exception = e; }

            Assert.NotNull(sut.Create(exception));
        }

        [Fact]
        public void Create_WithExceptionAndAttachStackTraceOptionOn_HasStackTrace()
        {
            _fixture.SentryOptions.AttachStacktrace = true;
            var sut = _fixture.GetSut();

            Exception exception;
            try
            {
                Throw();
                void Throw() => throw null;
            }
            catch (Exception e) { exception = e; }

            var stackTrace = sut.Create(exception);

            Assert.Equal(new StackTrace(exception, true).FrameCount, stackTrace?.Frames.Count);
        }

        [Theory]
        [InlineData(StackTraceMode.Original, "ByRefMethodThatThrows")]
        [InlineData(StackTraceMode.Enhanced, "(Fixture f, int b) SentryStackTraceFactoryTests.ByRefMethodThatThrows(int value, in int valueIn, ref int valueRef, out int valueOut)")]
        public void Create_InlineCase_IncludesAmpersandAfterParameterType(StackTraceMode mode, string method)
        {
            _fixture.SentryOptions.StackTraceMode = mode;

            // Arrange
            var i = 5;
            var exception = Record.Exception(() => ByRefMethodThatThrows(i, in i, ref i, out i));

            _fixture.SentryOptions.AttachStacktrace = true;
            var factory = _fixture.GetSut();

            // Act
            var stackTrace = factory.Create(exception);

            // Assert
            var frame = stackTrace!.Frames.Last();
            frame.Function.Should().Be(method);
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

        [Fact]
        public void CreateSentryStackFrame_NamespaceIncludedAndExcluded_IncludesTakesPrecedence()
        {
            _fixture.SentryOptions.AddInAppExclude(GetType().Namespace);
            _fixture.SentryOptions.AddInAppInclude(GetType().Namespace);
            var sut = _fixture.GetSut();
            var frame = new StackFrame();

            var actual = sut.CreateFrame(frame);

            Assert.True(actual.InApp);
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (Fixture f, int b) ByRefMethodThatThrows(int value, in int valueIn, ref int valueRef, out int valueOut) =>
            throw new Exception();
    }
}
