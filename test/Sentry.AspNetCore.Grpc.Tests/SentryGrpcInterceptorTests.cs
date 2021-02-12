using System;
using System.Threading.Tasks;
using Grpc.Core;
using NSubstitute;
using Xunit;

namespace Sentry.AspNetCore.Grpc.Tests
{
    public class SentryGrpcInterceptorTests
    {
        private class Fixture
        {
            public IHub Hub { get; set; } = Substitute.For<IHub>();
            public Func<IHub> HubAccessor { get; set; }
            public SentryAspNetCoreOptions Options { get; set; } = new();

            public ServerCallContext Context { get; set; } = Substitute.For<ServerCallContext>();

            public UnaryServerMethod<TestRequest, TestResponse> Continuation { get; set; } =
                (_, _) => Task.FromResult(new TestResponse());

            public Fixture()
            {
                HubAccessor = () => Hub;
                _ = Hub.IsEnabled.Returns(true);
            }

            public SentryGrpcInterceptor GetSut()
                => new(
                    HubAccessor,
                    Microsoft.Extensions.Options.Options.Create(Options));
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public async Task UnaryServerHandler_DisabledSdk_InvokesNextHandlers()
        {
            var request = new TestRequest();

            _ = _fixture.Hub.IsEnabled.Returns(false);
            _fixture.Continuation = Substitute.For<UnaryServerMethod<TestRequest, TestResponse>>();

            var sut = _fixture.GetSut();

            await sut.UnaryServerHandler(request, _fixture.Context, _fixture.Continuation);
            await _fixture.Continuation.Received(1).Invoke(request, _fixture.Context);
        }

        [Fact]
        public async Task UnaryServerHandler_DisabledSdk_NoScopePushed()
        {
            var request = new TestRequest();

            _ = _fixture.Hub.IsEnabled.Returns(false);

            var sut = _fixture.GetSut();

            await sut.UnaryServerHandler(request, _fixture.Context, _fixture.Continuation);

            _ = _fixture.Hub.DidNotReceive().PushScope();
        }


        [Fact]
        public async Task UnaryServerHandler_ExceptionThrown_SameRethrown()
        {
            var request = new TestRequest();

            var expected = new Exception("test");
            _fixture.Continuation = (_, _) => throw expected;

            var sut = _fixture.GetSut();

            var actual = await Assert.ThrowsAsync<Exception>(
                async () => await sut.UnaryServerHandler(request, _fixture.Context, _fixture.Continuation));

            Assert.Same(expected, actual);
        }


        [Fact]
        public async Task UnaryServerHandler_ScopePushedAndPopped_OnError()
        {
            var request = new TestRequest();

            var expected = new Exception("test");
            _fixture.Continuation = (_, _) => throw expected;
            var disposable = Substitute.For<IDisposable>();
            _ = _fixture.Hub.PushScope().Returns(disposable);

            var sut = _fixture.GetSut();

            _ = await Assert.ThrowsAsync<Exception>(
                async () => await sut.UnaryServerHandler(request, _fixture.Context, _fixture.Continuation));

            _ = _fixture.Hub.Received(1).PushScope();
            disposable.Received(1).Dispose();
        }
    }
}
