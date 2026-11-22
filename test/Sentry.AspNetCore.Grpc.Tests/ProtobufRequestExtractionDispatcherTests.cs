using Google.Protobuf;

namespace Sentry.AspNetCore.Grpc.Tests;

public class ProtobufRequestExtractionDispatcherTests
{
    private readonly SentryOptions _options = new();
    private readonly IProtobufRequest<TestRequest> _request = Substitute.For<IProtobufRequest<TestRequest>>();
    private readonly IProtobufRequestPayloadExtractor _extractor = Substitute.For<IProtobufRequestPayloadExtractor>();
    private readonly TestRequest _payload = new();
    private RequestSize _requestSize = RequestSize.Always;

    public ProtobufRequestExtractionDispatcherTests()
    {
        _request.ContentLength.Returns(10);
        _extractor.ExtractPayload(Arg.Any<IProtobufRequest<TestRequest>>()).Returns(_payload);
    }

    private ProtobufRequestExtractionDispatcher GetSut() => new([_extractor], _options, () => _requestSize);

    [Fact]
    public void ExtractPayload_DataCollectionNotConfigured_ReadsPayload()
    {
        var sut = GetSut();

        sut.ExtractPayload(_request).Should().BeSameAs(_payload);
    }

    [Theory]
    [InlineData(HttpBodyTypes.All, true)]
    [InlineData(HttpBodyTypes.IncomingRequest, true)]
    [InlineData(HttpBodyTypes.OutgoingRequest | HttpBodyTypes.IncomingResponse | HttpBodyTypes.OutgoingResponse, false)]
    [InlineData(HttpBodyTypes.None, false)]
    public void ExtractPayload_DataCollectionHttpBodies_GatesExtraction(HttpBodyTypes httpBodies, bool readBody)
    {
        _options.ConfigureDataCollection(dataCollection => dataCollection.HttpBodies = httpBodies);
        var sut = GetSut();

        IMessage actual = sut.ExtractPayload(_request);

        (actual is not null).Should().Be(readBody);
    }

    [Fact]
    public void ExtractPayload_DataCollectionIncludesIncomingRequest_RequestSizeStillApplies()
    {
        _requestSize = RequestSize.None;
        _options.ConfigureDataCollection(dataCollection => dataCollection.HttpBodies = HttpBodyTypes.All);
        var sut = GetSut();

        sut.ExtractPayload(_request).Should().BeNull();
    }
}
