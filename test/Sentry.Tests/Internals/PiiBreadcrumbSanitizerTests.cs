using FluentAssertions.Execution;

namespace Sentry.Tests.Internals;

public class PiiBreadcrumbSanitizerTests
{
    [Fact]
    public void Sanitize_Urls()
    {
        // Arrange
        var breadcrumbData = new Dictionary<string, string>
        {
            {"url", "https://user@sentry.io"},
            {"method", "GET"},
            {"status_code", "403"}
        };
        var breadcrumb = new Breadcrumb(
            timestamp : DateTimeOffset.UtcNow,
            message : "message https://user@sentry.io",
            type : "fake_type",
            data : breadcrumbData,
            category : "fake_category",
            level : BreadcrumbLevel.Error
            );

        // Act
        var actual = breadcrumb.Sanitize();

        // Assert
        using (new AssertionScope())
        {
            actual.Should().NotBeNull();
            actual.Timestamp.Should().Be(breadcrumb.Timestamp);
            actual.Message.Should().Be(PiiUrlSanitizer.Sanitize(breadcrumb.Message)); // should be sanitized
            actual.Type.Should().Be(breadcrumb.Type);
            actual.Data?["url"].Should().Be(PiiUrlSanitizer.Sanitize(breadcrumb.Data?["url"])); // should be sanitized
            actual.Data?["method"].Should().Be(breadcrumb.Data?["method"]);
            actual.Data?["status_code"].Should().Be(breadcrumb.Data?["status_code"]);
            actual.Category.Should().Be(breadcrumb.Category);
            actual.Level.Should().Be(breadcrumb.Level);
        }
    }
}
