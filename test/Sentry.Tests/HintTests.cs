namespace Sentry.Tests;

public class HintTests : IDisposable
{
    private readonly string _testDirectory;

    public HintTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void AddAttachment_Files_AddsToHint()
    {
        // Arrange
        var attachmentPath1 = Path.Combine(_testDirectory, Path.GetTempFileName());
        var attachmentPath2 = Path.Combine(_testDirectory, Path.GetTempFileName());
        File.Create(attachmentPath1);
        File.Create(attachmentPath2);

        var hint = new SentryHint(new SentryOptions());

        // Act
        hint.AddAttachment(attachmentPath1);
        hint.AddAttachment(attachmentPath2);

        // Assert
        Assert.Equal(2, hint.Attachments.Count);
    }

    [Fact]
    public void AddAttachment_ByteArray_AddsToHint()
    {
        // Arrange
        var byteArray1 = new byte[5];
        var byteArray2 = new byte[10];


        var hint = new SentryHint(new SentryOptions());

        // Act
        hint.AddAttachment(byteArray1, "byteArray1");
        hint.AddAttachment(byteArray2, "byteArray2");

        // Assert
        Assert.Equal(2, hint.Attachments.Count);
    }

    [Fact]
    public void Clear_WithEntries_ClearsHintEntries()
    {
        // Arrange
        var hint = new SentryHint("key", "value");

        // Act
        hint.Items.Clear();

        // Assert
        hint.Items.Count.Should().Be(0);
    }

    [Fact]
    public void ClearAttachments_WithAttachments_ClearsHintAttachments()
    {
        // Arrange
        var hint = new SentryHint();
        var attachment1 = Path.Combine(_testDirectory, Path.GetTempFileName());
        hint.AddAttachment(attachment1);

        // Act
        hint.Attachments.Clear();

        // Assert
        hint.Attachments.Should().BeEmpty();
    }

    [Fact]
    public void ContainsKey_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var hint = new SentryHint("key", "value");

        // Act
        var containsKey = hint.Items.ContainsKey("key");

        // Assert
        containsKey.Should().BeTrue();
    }

    [Fact]
    public void ContainsKey_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        var hint = new SentryHint("key", "value");

        // Act
        var containsKey = hint.Items.ContainsKey("nonExistingKey");

        // Assert
        containsKey.Should().BeFalse();
    }

    [Fact]
    public void Count_ReturnsZero_WhenHintIsEmpty()
    {
        // Arrange
        var hint = new SentryHint();

        // Act
        var count = hint.Items.Count;

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void Count_ReturnsCorrectValue_WhenHintHasItems()
    {
        // Arrange
        var hint = new SentryHint();
        hint.Items["key1"] = "value1";
        hint.Items["key2"] = "value2";

        // Act
        var count = hint.Items.Count;

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public void Remove_WithExistingKey_RemovesEntry()
    {
        // Arrange
        var hint = new SentryHint("key", "value");

        // Act
        hint.Items.Remove("key");

        // Assert
        hint.Items.ContainsKey("key").Should().BeFalse();
    }

    [Fact]
    public void WithAttachments_ReturnsHintWithAttachments()
    {
        // Arrange
        var attachment1 = AttachmentHelper.FakeAttachment("attachment1");
        var attachment2 = AttachmentHelper.FakeAttachment("attachment2");

        // Act
        var hint = SentryHint.WithAttachments(attachment1, attachment2);

        // Assert
        hint.Attachments.Count.Should().Be(2);
        hint.Attachments.Should().Contain(attachment1);
        hint.Attachments.Should().Contain(attachment2);
    }

    [Fact]
    public void WithAttachments_WithICollection_ReturnsHintWithAttachments()
    {
        // Arrange
        var attachment1 = AttachmentHelper.FakeAttachment("attachment1");
        var attachment2 = AttachmentHelper.FakeAttachment("attachment2");
        var attachments = new List<SentryAttachment> { attachment1, attachment2 };

        // Act
        var hint = SentryHint.WithAttachments(attachments);

        // Assert
        hint.Attachments.Count.Should().Be(2);
        hint.Attachments.Should().Contain(attachment1);
        hint.Attachments.Should().Contain(attachment2);
    }

    public void Dispose() => Directory.Delete(_testDirectory, true);
}
