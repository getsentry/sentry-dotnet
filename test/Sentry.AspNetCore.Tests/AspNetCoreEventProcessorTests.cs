using Microsoft.Extensions.Options;
using Sentry.Protocol;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class AspNetCoreEventProcessorTests
    {
        private readonly SentryAspNetCoreOptions _options = new SentryAspNetCoreOptions();
        private readonly AspNetCoreEventProcessor _sut;

        public AspNetCoreEventProcessorTests()
        {
            _sut = new AspNetCoreEventProcessor(Options.Create(_options));
        }

        [Fact]
        public void Process_WithRuntime_MovesToServerRuntime()
        {
            var target = new SentryEvent();
            var expected = target.Contexts.Runtime;

            _sut.Process(target);

            Assert.Same(expected, target.Contexts["server-runtime"]);
        }
        [Fact]
        public void Process_WithoutRuntime_NoServerRuntime()
        {
            var target = new SentryEvent();
            target.Contexts.TryRemove(Runtime.Type, out _);

            _sut.Process(target);

            Assert.False(target.Contexts.ContainsKey("server-runtime"));
        }

        [Fact]
        public void Process_WithOperatingSystem_MovesToServerOperatingSystem()
        {
            var target = new SentryEvent();
            var expected = target.Contexts.OperatingSystem;

            _sut.Process(target);

            Assert.Same(expected, target.Contexts["server-os"]);
        }

        [Fact]
        public void Process_WithoutOperatingSystem_NoServerOperatingSystem()
        {
            var target = new SentryEvent();
            target.Contexts.TryRemove(OperatingSystem.Type, out _);

            _sut.Process(target);

            Assert.False(target.Contexts.ContainsKey("server-os"));
        }


        [Fact]
        public void Process_AppliesDefaultTags()
        {
            const string key = "key";
            const string expected = "default tag value";
            var target = new SentryEvent();
            _options.DefaultTags[key] = expected;

            _sut.Process(target);

            Assert.Equal(expected, target.Tags[key]);
        }
    }
}
