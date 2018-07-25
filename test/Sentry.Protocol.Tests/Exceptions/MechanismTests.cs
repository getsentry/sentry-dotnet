using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol.Tests.Exceptions
{
    public class MechanismTests
    {
        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var sut = new Mechanism
            {
                Type = "mechanism type",
                Description = "mechanism description",
                Handled = true,
                HelpLink = "https://helplink",
                Data = ImmutableDictionary.Create<string, object>().Add("data-key", "data-value"),
                Meta = ImmutableDictionary.Create<string, object>().Add("meta-key", "meta-value"),
            };

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"type\":\"mechanism type\","
                        + "\"description\":\"mechanism description\","
                        + "\"help_link\":\"https://helplink\","
                        + "\"handled\":true,"
                        + "\"meta\":{\"meta-key\":\"meta-value\"},"
                        + "\"data\":{\"data-key\":\"data-value\"}}",
                    actual);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void SerializeObject_TestCase_SerializesAsExpected((Mechanism mechanism, string serialized) @case)
        {
            var actual = JsonSerializer.SerializeObject(@case.mechanism);

            Assert.Equal(@case.serialized, actual);
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { (new Mechanism(), "{}") };
            yield return new object[] { (new Mechanism { Type = "some type" }, "{\"type\":\"some type\"}") };
            yield return new object[] { (new Mechanism { Handled = false }, "{\"handled\":false}") };
            yield return new object[] { (new Mechanism { HelpLink = "https://sentry.io/docs" }, "{\"help_link\":\"https://sentry.io/docs\"}") };
            yield return new object[] { (new Mechanism { Description = "some desc" }, "{\"description\":\"some desc\"}") };
            yield return new object[] { (new Mechanism { Data = ImmutableDictionary.Create<string, object>().Add("data-key", "data-value") },
                "{\"data\":{\"data-key\":\"data-value\"}}") };
            yield return new object[] { (new Mechanism { Meta = ImmutableDictionary.Create<string, object>().Add("meta-key", "meta-value") },
                "{\"meta\":{\"meta-key\":\"meta-value\"}}") };
        }
    }
}
