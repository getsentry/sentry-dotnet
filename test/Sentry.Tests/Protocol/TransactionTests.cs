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
            var transaction = new Transaction(DisabledHub.Instance, "name123", "op123")
            {
                Description = "description",
                Request = new Request {Method = "GET", Url = "https://example.com"},
                User = new User {Email = "test@sentry.example", Username = "john"},
                Environment = "release",
                Fingerprint = new[] {"foo", "bar"},
                Sdk = new SdkVersion {Name = "SDK", Version = "1.1.1"}
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
