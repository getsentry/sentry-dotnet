using Microsoft.Extensions.Logging;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class EventIdExtensionsTests
    {
        [Fact]
        public void ToTupleOrNull_EmptyEventId_ReturnsNull()
        {
            EventId sut = default;
            Assert.Null(sut.ToTupleOrNull());
        }

        [Fact]
        public void ToTupleOrNull_EventId_ReturnsId()
        {
            const int expectedId = int.MaxValue;
            var sut = new EventId(expectedId);
            Assert.Equal((EventIdExtensions.DataKey, expectedId.ToString()), sut.ToTupleOrNull());
        }

        [Fact]
        public void ToTupleOrNull_EventIdAndName_ReturnsName()
        {
            const int id = int.MaxValue;
            const string expectedName = "name";

            var sut = new EventId(id, expectedName);
            Assert.Equal((EventIdExtensions.DataKey, expectedName), sut.ToTupleOrNull());
        }
    }
}
