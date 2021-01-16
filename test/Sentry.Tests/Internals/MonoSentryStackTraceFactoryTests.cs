using System;
using System.Diagnostics;
using System.Linq;
using Sentry;
using Sentry.Extensibility;
using Sentry.Internal;
using Xunit;

// ReSharper disable once CheckNamespace
// Stack trace filters out Sentry frames by namespace
namespace Other.Tests.Internals
{
    public class MonoSentryStackTraceFactoryTests
    {
        private class Fixture
        {
            public SentryOptions SentryOptions { get; set; } = new();
            public MonoSentryStackTraceFactory GetSut() => new(SentryOptions);
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public void Create_UnityIl2Cpp_LeavesOutZeroedOutPath()
        {
            var sut = _fixture.GetSut();
            var ex = new TestStackTraceException(@"  at UnityEngine.ProcessTouchPress (UnityEngine.EventSystems.PointerEventData pointerEvent, System.Boolean pressed, System.Boolean released) [0x00000] in <00000000000000000000000000000000>:0
  at UnityEngine.EventSystems.StandaloneInputModule.Process () [0x00000] in <00000000000000000000000000000000>:0 ");

            var actual = sut.Create(ex);
            Assert.Equal("Process ()", actual!.Frames[0].Function);
            Assert.Equal("UnityEngine.EventSystems.StandaloneInputModule", actual!.Frames[0].Module);
            Assert.Equal("ProcessTouchPress (UnityEngine.EventSystems.PointerEventData pointerEvent, System.Boolean pressed, System.Boolean released)", actual!.Frames[1].Function);
            Assert.Equal("UnityEngine", actual!.Frames[1].Module);

            for (var i = 0; i < 2; i++)
            {
                Assert.Null(actual!.Frames[i].ColumnNumber);
                Assert.Null(actual!.Frames[i].LineNumber);
                Assert.Null(actual!.Frames[i].InstructionOffset);
                Assert.Null(actual!.Frames[i].Package);
                Assert.Null(actual!.Frames[i].Platform);
                Assert.Null(actual!.Frames[i].InternalVars);
                Assert.Null(actual!.Frames[i].AbsolutePath);
                Assert.Null(actual!.Frames[i].ContextLine);
                Assert.Null(actual!.Frames[i].FileName);
                Assert.Equal(0, actual!.Frames[i].ImageAddress);
            }
        }

        [Fact]
        public void Create_NoExceptionAndDefaultAttachStackTraceOption_NullResult()
        {
            var sut = _fixture.GetSut();

            Assert.Null(sut.Create());
        }

        [Fact]
        public void Create_WithExceptionAndDefaultAttachStackTraceOption_HasStackTrace()
        {
            var sut = _fixture.GetSut();

            Exception exception;
            try
            {
                Throw();
                static void Throw() => throw null!;
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
                static void Throw() => throw null!;
            }
            catch (Exception e) { exception = e; }

            var stackTrace = sut.Create(exception);

            Assert.Equal(new StackTrace(exception, true).FrameCount, stackTrace!.Frames.Count);
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
            _fixture.SentryOptions.AddInAppExclude(GetType().Namespace!);
            var sut = _fixture.GetSut();
            var frame = new StackFrame();

            var actual = sut.CreateFrame(frame);

            Assert.False(actual.InApp);
        }

        [Fact]
        public void CreateSentryStackFrame_NamespaceIncludedAndExcluded_IncludesTakesPrecedence()
        {
            _fixture.SentryOptions.AddInAppExclude(GetType().Namespace!);
            _fixture.SentryOptions.AddInAppInclude(GetType().Namespace!);
            var sut = _fixture.GetSut();
            var frame = new StackFrame();

            var actual = sut.CreateFrame(frame);

            Assert.True(actual.InApp);
        }

        private class TestStackTraceException : Exception
        {
            public TestStackTraceException(string stacktrace) => StackTrace = stacktrace;

            public TestStackTraceException() : base()
            {
            }

            public TestStackTraceException(string message, Exception innerException) : base(message, innerException)
            {
            }

            public override string StackTrace { get; }
        }
    }
}
