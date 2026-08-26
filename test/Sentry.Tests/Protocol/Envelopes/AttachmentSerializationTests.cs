namespace Sentry.Tests.Protocol.Envelopes;

public partial class AttachmentSerializationTests
{
    private readonly IDiagnosticLogger _testOutputLogger;
    private readonly MockClock _fakeClock;

    public AttachmentSerializationTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
        _fakeClock = new MockClock(DateTimeOffset.MaxValue);
    }

    [Fact]
    public async Task Serialization_SameByteAttachmentEnvelopeTwice_PreservesPayload()
    {
        // Arrange
        var attachment = new SentryAttachment(
            AttachmentType.Default,
            new ByteAttachmentContent("test attachment content"u8.ToArray()),
            "test.txt",
            "text/plain");

        using var envelope = Envelope.FromAttachment(SentryId.Create(), attachment);

        // Act
        var firstSerialization = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);
        var secondSerialization = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        firstSerialization.Should().Contain("test attachment content");
        secondSerialization.Should().Be(firstSerialization);
    }

    [Fact]
    public async Task Serialization_SameEventEnvelopeWithByteAttachmentTwice_PreservesPayload()
    {
        // Arrange
        var attachment = new SentryAttachment(
            AttachmentType.Default,
            new ByteAttachmentContent("test attachment content"u8.ToArray()),
            "test.txt",
            "text/plain");

        using var envelope = Envelope.FromEvent(new SentryEvent(), attachments: [attachment]);

        // Act
        var firstSerialization = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);
        var secondSerialization = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        envelope.Items.Count(item => item.TryGetType() == EnvelopeItem.TypeValueAttachment).Should().Be(1);
        firstSerialization.Should().Contain("test attachment content");
        secondSerialization.Should().Be(firstSerialization);
    }

    [Fact]
    public async Task Serialization_SameEventEnvelopeWithFileAttachmentTwice_PreservesPayload()
    {
        // Arrange
        var path = Path.GetTempFileName();
        const string attachmentContent = "test attachment content";
        File.WriteAllText(path, attachmentContent);

        try
        {
            var attachment = new SentryAttachment(
                AttachmentType.Default,
                new FileAttachmentContent(path),
                "test.txt",
                "text/plain");

            using var envelope = Envelope.FromEvent(new SentryEvent(), attachments: [attachment]);

            // Act
            var firstSerialization = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);
            var secondSerialization = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

            // Assert
            firstSerialization.Should().Contain(attachmentContent);
            secondSerialization.Should().Be(firstSerialization);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Serialization_SameEventEnvelopeWithStreamAttachmentTwice_PreservesPayload()
    {
        // Arrange
        const string attachmentContent = "test attachment content";
        using var attachmentStream = new MemoryStream(Encoding.UTF8.GetBytes(attachmentContent));
        var attachment = new SentryAttachment(
            AttachmentType.Default,
            new StreamAttachmentContent(attachmentStream),
            "test.txt",
            "text/plain");

        using var envelope = Envelope.FromEvent(new SentryEvent(), attachments: [attachment]);

        // Act
        var firstSerialization = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);
        var secondSerialization = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        firstSerialization.Should().Contain(attachmentContent);
        secondSerialization.Should().Be(firstSerialization);
    }

    [Fact]
    public async Task Serialization_SameEventEnvelopeWithStreamAttachmentConcurrently_PreservesPayload()
    {
        // Arrange
        const string attachmentContent = "test attachment content";
        using var attachmentStream = new MemoryStream(Encoding.UTF8.GetBytes(attachmentContent));
        var attachment = new SentryAttachment(
            AttachmentType.Default,
            new StreamAttachmentContent(attachmentStream),
            "test.txt",
            "text/plain");

        using var envelope = Envelope.FromEvent(new SentryEvent(), attachments: [attachment]);

        // Act
        var serializations = await Task.WhenAll(
            envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock),
            envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock));

        // Assert
        serializations[0].Should().Contain(attachmentContent);
        serializations[1].Should().Be(serializations[0]);
    }

    [Fact]
    public async Task Serialization_NonSeekableStreamAttachmentTwice_PreservesPayload()
    {
        // Arrange
        const string attachmentContent = "test attachment content";
        using var attachmentStream = new NonSeekableReadStream(
            new MemoryStream(Encoding.UTF8.GetBytes(attachmentContent)));
        var attachment = new SentryAttachment(
            AttachmentType.Default,
            new StreamAttachmentContent(attachmentStream),
            "test.txt",
            "text/plain");

        using var envelope = Envelope.FromEvent(new SentryEvent(), attachments: [attachment]);

        // Act
        var firstSerialization = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);
        var secondSerialization = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        firstSerialization.Should().Contain(attachmentContent);
        secondSerialization.Should().Be(firstSerialization);
    }

    [Fact]
    public void Serialization_SameEventEnvelopeWithStreamAttachmentTwiceSynchronously_PreservesPayload()
    {
        // Arrange
        const string attachmentContent = "test attachment content";
        using var attachmentStream = new MemoryStream(Encoding.UTF8.GetBytes(attachmentContent));
        var attachment = new SentryAttachment(
            AttachmentType.Default,
            new StreamAttachmentContent(attachmentStream),
            "test.txt",
            "text/plain");

        using var envelope = Envelope.FromEvent(new SentryEvent(), attachments: [attachment]);

        // Act
        var firstSerialization = envelope.SerializeToString(_testOutputLogger, _fakeClock);
        var secondSerialization = envelope.SerializeToString(_testOutputLogger, _fakeClock);

        // Assert
        firstSerialization.Should().Contain(attachmentContent);
        secondSerialization.Should().Be(firstSerialization);
    }

    [Fact]
    public async Task Serialization_SameEventEnvelopeWithDeleteOnCloseFileAttachmentTwice_PreservesPayload()
    {
        // Arrange
        var path = Path.GetTempFileName();
        const string attachmentContent = "test attachment content";
        File.WriteAllText(path, attachmentContent);

        try
        {
            var attachment = new SentryAttachment(
                AttachmentType.Default,
                new FileAttachmentContent(
                    path,
                    readFileAsynchronously: false,
                    deleteOnClose: true),
                "test.txt",
                "text/plain");

            using var envelope = Envelope.FromEvent(new SentryEvent(), attachments: [attachment]);

            // Act
            var firstSerialization = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);
            var secondSerialization = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

            // Assert
            firstSerialization.Should().Contain(attachmentContent);
            secondSerialization.Should().Be(firstSerialization);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FromEvent_StreamAttachment_DoesNotReadStream()
    {
        // Arrange
        const string attachmentContent = "test attachment content";
        using var attachmentStream = new MemoryStream(Encoding.UTF8.GetBytes(attachmentContent));
        var attachment = new SentryAttachment(
            AttachmentType.Default,
            new StreamAttachmentContent(attachmentStream),
            "test.txt",
            "text/plain");

        // Act
        using var envelope = Envelope.FromEvent(new SentryEvent(), attachments: [attachment]);

        // Assert
        attachmentStream.Position.Should().Be(0);
    }

    [Fact]
    public async Task Serialization_SameEventEnvelopeWithCustomAttachmentContentTwice_PreservesPayload()
    {
        // Arrange
        const string attachmentContent = "test attachment content";
        using var attachmentStream = new MemoryStream(Encoding.UTF8.GetBytes(attachmentContent));
        var attachment = new SentryAttachment(
            AttachmentType.Default,
            new SingleStreamAttachmentContent(attachmentStream),
            "test.txt",
            "text/plain");

        using var envelope = Envelope.FromEvent(
            new SentryEvent(),
            attachments: [attachment]);

        // Act
        var firstSerialization = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);
        var secondSerialization = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        firstSerialization.Should().Contain(attachmentContent);
        secondSerialization.Should().Be(firstSerialization);
    }

    [Fact]
    public async Task Serialization_AsyncOnlyStreamAttachment_Succeeds()
    {
        // Arrange
        const string attachmentContent = "test attachment content";
        using var attachmentStream =
            new AsyncOnlyReadStream(Encoding.UTF8.GetBytes(attachmentContent));
        var attachment = new SentryAttachment(
            AttachmentType.Default,
            new StreamAttachmentContent(attachmentStream),
            "test.txt",
            "text/plain");

        using var envelope = Envelope.FromEvent(
            new SentryEvent(),
            attachments: [attachment]);

        // Act
        var serialization =
            await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        serialization.Should().Contain(attachmentContent);
    }

    [Fact]
    public async Task Serialization_SameEnvelopeWithDerivedByteAttachmentContentTwice_PreservesPayload()
    {
        // Arrange
        const string attachmentContent = "test attachment content";
        var bytes = Encoding.UTF8.GetBytes(attachmentContent);
        using var attachmentStream = new MemoryStream(bytes);
        var attachment = new SentryAttachment(
            AttachmentType.Default,
            new SingleStreamDerivedByteAttachmentContent(bytes, attachmentStream),
            "test.txt",
            "text/plain");

        using var envelope = Envelope.FromEvent(
            new SentryEvent(),
            attachments: [attachment]);

        // Act
        var firstSerialization =
            await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);
        var secondSerialization =
            await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        firstSerialization.Should().Contain(attachmentContent);
        secondSerialization.Should().Be(firstSerialization);
    }
}
