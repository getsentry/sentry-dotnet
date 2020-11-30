using System.Collections.Generic;
using FluentAssertions;
using Sentry.Internal;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol.Tests.Context
{
    public class BrowserTests
    {
        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var sut = new Browser
            {
                Version = "6",
                Name = "Internet Explorer",
            };

            var actualString = sut.ToJsonString();

            var actual = Browser.FromJson(Json.Parse(actualString));
            actual.Should().BeEquivalentTo(sut);
        }

        [Fact]
        public void Clone_CopyValues()
        {
            var sut = new Browser()
            {
                Name = "name",
                Version = "version"
            };

            var clone = sut.Clone();

            Assert.Equal(sut.Name, clone.Name);
            Assert.Equal(sut.Version, clone.Version);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void SerializeObject_TestCase_SerializesAsExpected((Browser browser, string serialized) @case)
        {
            var actual = @case.browser.ToJsonString();

            Assert.Equal(@case.serialized, actual);
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { (new Browser(), "{\"type\":\"browser\"}") };
            yield return new object[] { (new Browser { Name = "some name" }, "{\"type\":\"browser\",\"name\":\"some name\"}") };
            yield return new object[] { (new Browser { Version = "some version" }, "{\"type\":\"browser\",\"version\":\"some version\"}") };
        }
    }
}
