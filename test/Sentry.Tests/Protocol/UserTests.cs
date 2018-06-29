using System;
using System.Collections.Generic;
using Sentry.Internal;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol
{
    public class UserTests
    {
        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var sut = new User
            (
                id: "user-id",
                email: "test@sentry.io",
                ipAddress: "::1",
                username: "user-name"
            );

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"email\":\"test@sentry.io\","
                        + "\"id\":\"user-id\","
                        + "\"ip_address\":\"::1\","
                        + "\"username\":\"user-name\"}",
                    actual);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void SerializeObject_TestCase_SerializesAsExpected((User user, string serialized) @case)
        {
            var actual = JsonSerializer.SerializeObject(@case.user);

            Assert.Equal(@case.serialized, actual);
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { (new User(), "{}") };
            yield return new object[] { (new User(id: "some id"), "{\"id\":\"some id\"}") };
            yield return new object[] { (new User(email: "some email"), "{\"email\":\"some email\"}") };
            yield return new object[] { (new User(ipAddress: "some ipAddress"), "{\"ip_address\":\"some ipAddress\"}") };
            yield return new object[] { (new User(username: "some username"), "{\"username\":\"some username\"}") };
        }
    }
}
