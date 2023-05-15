using FluentAssertions.Execution;

namespace Sentry.Tests;

public class TransactionTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public TransactionTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void Redact_Redacts_Urls()
    {
        // Arrange
        var name = "name123 https://user@not.redacted";
        var operation = "op123 https://user@not.redacted";
        var description = "desc123 https://user@sentry.io"; // should be redacted
        var platform = "platform123 https://user@not.redacted";
        var release = "release123 https://user@not.redacted";
        var distribution = "distribution123 https://user@not.redacted";
        var environment = "environment123 https://user@not.redacted";
        var breadcrumbMessage = "message https://user@sentry.io"; // should be redacted
        var breadcrumbDataValue = "data-value https://user@sentry.io"; // should be redacted
        var tagValue = "tag_value https://user@not.redacted";

        var timestamp = DateTimeOffset.MaxValue;

        var transaction = new Transaction(name, operation, TransactionNameSource.Url)
        {
            Description = description,
            Platform = platform,
            Release = release,
            Distribution = distribution,
            // Request = Request,
            // User = User,
            Environment = environment
        };

        transaction.AddBreadcrumb(new Breadcrumb(timestamp, breadcrumbMessage));
        transaction.AddBreadcrumb(new Breadcrumb(
            timestamp,
            "message",
            "type",
            new Dictionary<string, string> { { "data-key", breadcrumbDataValue } },
            "category",
            BreadcrumbLevel.Warning));
        transaction.SetTag("tag_key", tagValue);

        // Act
        transaction.Redact();

        // Assert
        using (new AssertionScope())
        {
            transaction.Name.Should().Be(name);
            transaction.Operation.Should().Be(operation);
            transaction.Description.Should().Be($"desc123 https://{PiiExtensions.RedactedText}@sentry.io");
            transaction.Platform.Should().Be(platform);
            transaction.Release.Should().Be(release);
            transaction.Distribution.Should().Be(distribution);
            transaction.Environment.Should().Be(environment);
            var breadcrumbs = transaction.Breadcrumbs.ToArray();
            breadcrumbs.Length.Should().Be(2);
            breadcrumbs[0].Message.Should().Be($"message https://{PiiExtensions.RedactedText}@sentry.io");
            breadcrumbs[1].Data?["data-key"].Should().Be($"data-value https://{PiiExtensions.RedactedText}@sentry.io");
            transaction.Tags["tag_key"].Should().Be(tagValue);
        }
    }
}
