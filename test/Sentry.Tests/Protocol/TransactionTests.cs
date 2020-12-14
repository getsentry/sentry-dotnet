using FluentAssertions;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol
{
    public class TransactionTests
    {
        [Fact]
        public void Serialization_Roundtrip_Success()
        {
            // Arrange
            var transaction = new Transaction(DisabledHub.Instance, null)
            {
                Name = "my transaction",
                Operation = "some op",
                Description = "description",
                Request = new Request {Method = "GET", Url = "https://test.com"},
                User = new User {Email = "foo@bar.com", Username = "john"},
                Environment = "release",
                Fingerprint = new[] {"foo", "bar"}
            };

            transaction.Finish();

            // Act
            var json = transaction.ToJsonString();
            var transactionRoundtrip = Transaction.FromJson(Json.Parse(json));

            // Assert
            transactionRoundtrip.Should().BeEquivalentTo(transaction);
        }
    }
}
