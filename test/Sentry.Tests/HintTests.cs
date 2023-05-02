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
        var hint = new Hint();
        hint["key"] = "value";

        // Act
        hint.Clear();

        // Assert
        hint.Count.Should().Be(0);
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
        var hint = new Hint();
        hint["key"] = "value";

        // Act
        var containsKey = hint.ContainsKey("key");

        // Assert
        containsKey.Should().BeTrue();
    }

    [Fact]
    public void ContainsKey_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        var hint = new Hint();
        hint["key"] = "value";

        // Act
        var containsKey = hint.ContainsKey("nonExistingKey");

        // Assert
        containsKey.Should().BeFalse();
    }

    [Fact]
    public void CopyTo_EmptyHint_CopyToArray()
    {
        // Arrange
        var hint = new Hint();
        var array = new object[3];

        // Act
        hint.CopyTo(array, 0);

        // Assert
        array.Should().BeEquivalentTo(new object[] { null, null, null });
    }

    [Fact]
    public void CopyTo_NonEmptyHint_CopyToArray()
    {
        // Arrange
        var hint = new Hint();
        hint["key1"] = "value1";
        hint["key2"] = "value2";
        hint["key3"] = "value3";
        var array = new object[3];

        // Act
        hint.CopyTo(array, 0);

        // Assert
        array.Should().BeEquivalentTo(new [] {
            new KeyValuePair<string, object>("key1", "value1"),
            new KeyValuePair<string, object>("key2", "value2"),
            new KeyValuePair<string, object>("key3", "value3"),
        });
    }

    [Fact]
    public void CopyTo_ArrayTooSmall_ThrowsException()
    {
        // Arrange
        var hint = new Hint();
        hint["key1"] = "value1";
        hint["key2"] = "value2";
        hint["key3"] = "value3";
        var array = new object[2];

        // Act and Assert
        Assert.Throws<System.ArgumentException>(() => hint.CopyTo(array, 0));
    }

    [Fact]
    public void Count_ReturnsZero_WhenHintIsEmpty()
    {
        // Arrange
        var hint = new Hint();

        // Act
        var count = hint.Count;

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void Count_ReturnsCorrectValue_WhenHintHasItems()
    {
        // Arrange
        var hint = new Hint();
        hint["key1"] = "value1";
        hint["key2"] = "value2";

        // Act
        var count = hint.Count;

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public void GetValue_WithNonExistingKey_ReturnsNull()
    {
        // Arrange
        var hint = new Hint();

        // Act
        var result = hint.GetValue<string>("non-existing");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetValue_WithExistingKey_ReturnsValue()
    {
        // Arrange
        var hint = new Hint();
        hint["key"] = "value";

        // Act
        var result = hint.GetValue<string>("key");

        // Assert
        Assert.Equal("value", result);
    }

    [Fact]
    public void GetEnumerator_ReturnsValidEnumreator()
    {
        // Arrange
        var hint = new Hint();
        hint["key1"] = "value1";
        hint["key2"] = "value2";

        // Act + Assert
        var enumerator = hint.GetEnumerator();

        // Assert
        enumerator.Should().BeAssignableTo<IEnumerator<KeyValuePair<string, object>>>();
        while (enumerator.MoveNext())
        {
            enumerator.Current.Key.Should().BeOneOf("key1", "key2");
            enumerator.Current.Value.Should().BeOneOf("value1", "value2");
        }
    }

    [Fact]
    public void Remove_WithExistingKey_RemovesEntry()
    {
        // Arrange
        var hint = new Hint();
        hint["key"] = "value";

        // Act
        hint.Remove("key");

        // Assert
        Assert.Null(hint["key"]);
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
