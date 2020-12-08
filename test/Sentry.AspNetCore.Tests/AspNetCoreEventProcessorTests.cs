using System;
using Sentry.Protocol;
using Xunit;
using OperatingSystem = Sentry.Protocol.OperatingSystem;

namespace Sentry.AspNetCore.Tests
{
    public class AspNetCoreEventProcessorTests
    {
        private readonly AspNetCoreEventProcessor _sut;

        public AspNetCoreEventProcessorTests()
        {
            _sut = new AspNetCoreEventProcessor();
        }

        [Fact]
        public void Process_WithRuntime_MovesToServerRuntime()
        {
            var target = new SentryEvent();
            var expected = target.Contexts.Runtime;

            _ = _sut.Process(target);

            Assert.Same(expected, target.Contexts["server-runtime"]);
        }
        [Fact]
        public void Process_WithoutRuntime_NoServerRuntime()
        {
            var target = new SentryEvent();
            _ = target.Contexts.TryRemove(Runtime.Type, out _);

            _ = _sut.Process(target);

            Assert.False(target.Contexts.ContainsKey("server-runtime"));
        }

        [Fact]
        public void Process_WithOperatingSystem_MovesToServerOperatingSystem()
        {
            var target = new SentryEvent();
            var expected = target.Contexts.OperatingSystem;

            _ = _sut.Process(target);

            Assert.Same(expected, target.Contexts["server-os"]);
        }

        [Fact]
        public void Process_WithoutOperatingSystem_NoServerOperatingSystem()
        {
            var target = new SentryEvent();
            _ = target.Contexts.TryRemove(OperatingSystem.Type, out _);

            _ = _sut.Process(target);

            Assert.False(target.Contexts.ContainsKey("server-os"));
        }

        [Fact]
        public void Process_ServerName_NotOverwritten()
        {
            var target = new SentryEvent();
            const string expectedServerName = "original";
            target.ServerName = expectedServerName;

            _ = _sut.Process(target);

            Assert.Equal(expectedServerName, target.ServerName);
        }

        [Fact]
        public void Process_ServerName_SetToEnvironmentMachineName()
        {
            var target = new SentryEvent();

            _ = _sut.Process(target);

            Assert.Equal(Environment.MachineName, target.ServerName);
        }
    }
}
