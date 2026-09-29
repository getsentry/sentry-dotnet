#if ANDROID
using Sentry.Android.Callbacks;

namespace Sentry.Tests.Platforms.Android;

public class BeforeBreadcrumbCallbackTests
{
    private static JavaSdk.Breadcrumb JavaBreadcrumb() => new() { Message = "test", Category = "test" };

    [Fact]
    public void Execute_CallbackThrows_ReturnsNullAndLogsError()
    {
        // Arrange
        var exception = new InvalidOperationException("callback failed");
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        using var sut = new BeforeBreadcrumbCallback((_, _) => throw exception, options);

        // Act
        using var result = sut.Execute(JavaBreadcrumb(), new JavaSdk.Hint());

        // Assert
        result.Should().BeNull();
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == SentryLevel.Error &&
            entry.Exception == exception &&
            entry.Message == "Android BeforeBreadcrumb callback failed.");
    }

    [Fact]
    public void Execute_CallbackReturnsNull_ReturnsNull()
    {
        // Arrange
        var options = new SentryOptions();
        using var sut = new BeforeBreadcrumbCallback((_, _) => null, options);

        // Act
        using var result = sut.Execute(JavaBreadcrumb(), new JavaSdk.Hint());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Execute_CallbackReturnsInput_ReturnsOriginalJavaBreadcrumb()
    {
        // Arrange
        var options = new SentryOptions();
        using var sut = new BeforeBreadcrumbCallback((breadcrumb, _) => breadcrumb, options);
        using var javaBreadcrumb = JavaBreadcrumb();

        // Act
        var result = sut.Execute(javaBreadcrumb, new JavaSdk.Hint());

        // Assert
        result.Should().BeSameAs(javaBreadcrumb);
    }
}
#endif
