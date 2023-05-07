namespace Sentry.Tests;

public class HintTests
{
    private Attachment FakeAttachment(string name = "test.txt")
        => new(
            AttachmentType.Default,
            new StreamAttachmentContent(new MemoryStream(new byte[] { 1 })),
            name,
            null
            );

    [Fact]
    public void AddAttachments_WithNullAttachments_DoesNothing()
    {
        // Arrange
        var hint = new Hint();

        // Act
        hint.AddAttachments(null);

        // Assert
        Assert.Empty(hint.Attachments);
    }

    [Fact]
    public void AddAttachments_WithAttachments_AddsToHint()
    {
        // Arrange
        var hint = new Hint();
        var attachment1 = FakeAttachment("attachment1");
        var attachment2 = FakeAttachment("attachment2");

        // Act
        hint.AddAttachments(attachment1, attachment2);

        // Assert
        Assert.Equal(2, hint.Attachments.Count);
    }

    [Fact]
    public void Clear_WithEntries_ClearsHintEntries()
    {
        // Arrange
        var hint = new Hint("key", "value");

        // Act
        hint.Items.Clear();

        // Assert
        hint.Items.Count.Should().Be(0);
    }

    [Fact]
    public void ClearAttachments_WithAttachments_ClearsHintAttachments()
    {
        // Arrange
        var hint = new Hint();
        var attachment1 = FakeAttachment("attachment1");
        hint.AddAttachments(attachment1);

        // Act
        hint.Attachments.Clear();

        // Assert
        hint.Attachments.Should().BeEmpty();
    }

    [Fact]
    public void ContainsKey_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var hint = new Hint("key", "value");

        // Act
        var containsKey = hint.Items.ContainsKey("key");

        // Assert
        containsKey.Should().BeTrue();
    }

    [Fact]
    public void ContainsKey_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        var hint = new Hint("key", "value");

        // Act
        var containsKey = hint.Items.ContainsKey("nonExistingKey");

        // Assert
        containsKey.Should().BeFalse();
    }

    [Fact]
    public void Count_ReturnsZero_WhenHintIsEmpty()
    {
        // Arrange
        var hint = new Hint();

        // Act
        var count = hint.Items.Count;

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void Count_ReturnsCorrectValue_WhenHintHasItems()
    {
        // Arrange
        var hint = new Hint();
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
        var hint = new Hint("key", "value");

        // Act
        hint.Items.Remove("key");

        // Assert
        hint.Items.ContainsKey("key").Should().BeFalse();
    }

    [Fact]
    public void Screenshot_PropertyCanBeSetAndRetrieved()
    {
        // Arrange
        var hint = new Hint();
        var screenshot = FakeAttachment("screenshot.png");

        // Act
        hint.Screenshot = screenshot;
        var retrievedScreenshot = hint.Screenshot;

        // Assert
        retrievedScreenshot.Should().Be(screenshot);
    }

    [Fact]
    public void ViewHierarchy_PropertyCanBeSetAndRetrieved()
    {
        // Arrange
        var hint = new Hint();
        var viewHierarchy = FakeAttachment("view_hierarchy.xml");

        // Act
        hint.ViewHierarchy = viewHierarchy;
        var retrievedViewHierarchy = hint.ViewHierarchy;

        // Assert
        retrievedViewHierarchy.Should().Be(viewHierarchy);
    }

    [Fact]
    public void WithAttachments_ReturnsHintWithAttachments()
    {
        // Arrange
        var attachment1 = FakeAttachment("attachment1");
        var attachment2 = FakeAttachment("attachment2");

        // Act
        var hint = Hint.WithAttachments(attachment1, attachment2);

        // Assert
        hint.Attachments.Count.Should().Be(2);
        hint.Attachments.Should().Contain(attachment1);
        hint.Attachments.Should().Contain(attachment2);
    }

    [Fact]
    public void WithAttachments_WithICollection_ReturnsHintWithAttachments()
    {
        // Arrange
        var attachment1 = FakeAttachment("attachment1");
        var attachment2 = FakeAttachment("attachment2");
        var attachments = new List<Attachment> { attachment1, attachment2 };

        // Act
        var hint = Hint.WithAttachments(attachments);

        // Assert
        hint.Attachments.Count.Should().Be(2);
        hint.Attachments.Should().Contain(attachment1);
        hint.Attachments.Should().Contain(attachment2);
    }
}
