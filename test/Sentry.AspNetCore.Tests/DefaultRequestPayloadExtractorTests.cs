using System.IO;
using Sentry.Extensibility;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class DefaultRequestPayloadExtractorTests : BaseRequestPayloadExtractorTests<DefaultRequestPayloadExtractor>
    {
        [Fact]
        public void ExtractPayload_StringData_ReadCorrectly()
        {
            const string expected = "The request payload: éãüçó";
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(expected);
            writer.Flush();
            TestFixture.Stream = stream;

            var sut = TestFixture.GetSut();

            var actual = sut.ExtractPayload(TestFixture.HttpRequest);

            Assert.Equal(expected, actual);
        }
    }
}
