using Sentry.Testing;

namespace Sentry.Tests.Protocol.Envelopes;

public class EnvelopeTests
{
    // Tests driven by examples from docs:
    // https://develop.sentry.dev/sdk/envelopes/#full-examples

    private readonly IDiagnosticLogger _testOutputLogger;
    private readonly MockClock _fakeClock;

    public EnvelopeTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
        _fakeClock = new MockClock(DateTimeOffset.MaxValue);
    }

    [Fact]
    public async Task Serialization_EnvelopeWithoutItems_Success()
    {
        // Arrange
        using var envelope = new Envelope(
            new Dictionary<string, object> { ["event_id"] = "12c2d058d58442709aa2eca08bf20986" },
            Array.Empty<EnvelopeItem>());

        // Act
        var output = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be(
            "{\"event_id\":\"12c2d058d58442709aa2eca08bf20986\",\"sent_at\":\"9999-12-31T23:59:59.9999999+00:00\"}\n");
    }

    [Fact]
    public void Serialization_EnvelopeWithoutItems_Success_Synchronous()
    {
        // Arrange
        using var envelope = new Envelope(
            new Dictionary<string, object> { ["event_id"] = "12c2d058d58442709aa2eca08bf20986" },
            Array.Empty<EnvelopeItem>());

        // Act
        var output = envelope.SerializeToString(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be(
            "{\"event_id\":\"12c2d058d58442709aa2eca08bf20986\",\"sent_at\":\"9999-12-31T23:59:59.9999999+00:00\"}\n");
    }

    [Fact]
    public async Task Deserialization_EnvelopeWithoutItems_Success()
    {
        // Arrange
        using var input = "{\"event_id\":\"12c2d058d58442709aa2eca08bf20986\"}\n".ToMemoryStream();

        using var expectedEnvelope = new Envelope(
            new Dictionary<string, object> { ["event_id"] = "12c2d058d58442709aa2eca08bf20986" },
            Array.Empty<EnvelopeItem>());

        // Act
        using var envelope = await Envelope.DeserializeAsync(input);

        // Assert
        envelope.Should().BeEquivalentTo(expectedEnvelope);
    }

    [Fact]
    public async Task Serialization_EnvelopeWithoutHeader_Success()
    {
        // Arrange
        using var envelope = new Envelope(
            new Dictionary<string, object>(),
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object>{["type"] = "session"},
                    new StreamSerializable("{\"started\": \"2020-02-07T14:16:00Z\",\"attrs\":{\"release\":\"sentry-test@1.0.0\"}}"
                        .ToMemoryStream())
                )
            });

        // Act
        var output = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be(
            "{\"sent_at\":\"9999-12-31T23:59:59.9999999+00:00\"}\n" +
            "{\"type\":\"session\",\"length\":75}\n" +
            "{\"started\": \"2020-02-07T14:16:00Z\",\"attrs\":{\"release\":\"sentry-test@1.0.0\"}}\n");
    }

    [Fact]
    public void Serialization_EnvelopeWithoutHeader_Success_Synchronous()
    {
        // Arrange
        using var envelope = new Envelope(
            new Dictionary<string, object>(),
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object>{["type"] = "session"},
                    new StreamSerializable("{\"started\": \"2020-02-07T14:16:00Z\",\"attrs\":{\"release\":\"sentry-test@1.0.0\"}}"
                        .ToMemoryStream())
                )
            });

        // Act
        var output = envelope.SerializeToString(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be(
            "{\"sent_at\":\"9999-12-31T23:59:59.9999999+00:00\"}\n" +
            "{\"type\":\"session\",\"length\":75}\n" +
            "{\"started\": \"2020-02-07T14:16:00Z\",\"attrs\":{\"release\":\"sentry-test@1.0.0\"}}\n");
    }

    [Fact]
    public async Task Deserialization_EnvelopeWithoutHeader_Success()
    {
        // Arrange
        using var input = (
                "{}\n" +
                "{\"type\":\"fake\",\"length\":75}\n" +
                "{\"started\": \"2020-02-07T14:16:00Z\",\"attrs\":{\"release\":\"sentry-test@1.0.0\"}}\n"
            ).ToMemoryStream();

        using var expectedEnvelope = new Envelope(
            new Dictionary<string, object>(),
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object>{["type"] = "fake", ["length"] = 75L},
                    new StreamSerializable("{\"started\": \"2020-02-07T14:16:00Z\",\"attrs\":{\"release\":\"sentry-test@1.0.0\"}}"
                        .ToMemoryStream())
                )
            });

        // Act
        using var envelope = await Envelope.DeserializeAsync(input);

        // Assert
        envelope.Should().BeEquivalentTo(expectedEnvelope);
    }

    [Fact]
    public async Task Serialization_EnvelopeWithTwoItems_Success()
    {
        // Arrange
        using var envelope = new Envelope(
            new Dictionary<string, object>
            {
                ["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc",
                ["dsn"] = "https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42"
            },
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "attachment",
                        ["length"] = 13,
                        ["content_type"] = "text/plain",
                        ["filename"] = "hello.txt"
                    },
                    new StreamSerializable("\xef\xbb\xbfHello\r\n".ToMemoryStream())
                ),

                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "event",
                        ["length"] = 41,
                        ["content_type"] = "application/json",
                        ["filename"] = "application.log"
                    },
                    new StreamSerializable("{\"message\":\"hello world\",\"level\":\"error\"}".ToMemoryStream())
                )
            });

        // Act
        var output = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be(
            "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\",\"dsn\":\"https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42\",\"sent_at\":\"9999-12-31T23:59:59.9999999+00:00\"}\n" +
            "{\"type\":\"attachment\",\"length\":13,\"content_type\":\"text/plain\",\"filename\":\"hello.txt\"}\n" +
            "\xef\xbb\xbfHello\r\n\n" +
            "{\"type\":\"event\",\"length\":41,\"content_type\":\"application/json\",\"filename\":\"application.log\"}\n" +
            "{\"message\":\"hello world\",\"level\":\"error\"}\n");
    }

    [Fact]
    public void Serialization_EnvelopeWithTwoItems_Success_Synchronous()
    {
        // Arrange
        using var envelope = new Envelope(
            new Dictionary<string, object>
            {
                ["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc",
                ["dsn"] = "https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42"
            },
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "attachment",
                        ["length"] = 13,
                        ["content_type"] = "text/plain",
                        ["filename"] = "hello.txt"
                    },
                    new StreamSerializable("\xef\xbb\xbfHello\r\n".ToMemoryStream())
                ),

                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "event",
                        ["length"] = 41,
                        ["content_type"] = "application/json",
                        ["filename"] = "application.log"
                    },
                    new StreamSerializable("{\"message\":\"hello world\",\"level\":\"error\"}".ToMemoryStream())
                )
            });

        // Act
        var output = envelope.SerializeToString(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be(
            "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\",\"dsn\":\"https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42\",\"sent_at\":\"9999-12-31T23:59:59.9999999+00:00\"}\n" +
            "{\"type\":\"attachment\",\"length\":13,\"content_type\":\"text/plain\",\"filename\":\"hello.txt\"}\n" +
            "\xef\xbb\xbfHello\r\n\n" +
            "{\"type\":\"event\",\"length\":41,\"content_type\":\"application/json\",\"filename\":\"application.log\"}\n" +
            "{\"message\":\"hello world\",\"level\":\"error\"}\n");
    }

    [Fact]
    public async Task Deserialization_EnvelopeWithTwoItems_Success()
    {
        using var input = (
                "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\",\"dsn\":\"https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42\"}\n" +
                "{\"type\":\"attachment\",\"length\":13,\"content_type\":\"text/plain\",\"filename\":\"hello.txt\"}\n" +
                "\xef\xbb\xbfHello\r\n\n" +
                "{\"type\":\"event\",\"length\":41,\"content_type\":\"application/json\",\"filename\":\"application.log\"}\n" +
                "{\"message\":\"hello world\",\"level\":\"error\"}\n"
            ).ToMemoryStream();

        using var expectedEnvelope = new Envelope(
            new Dictionary<string, object>
            {
                ["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc",
                ["dsn"] = "https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42"
            },
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "attachment",
                        ["length"] = 13L,
                        ["content_type"] = "text/plain",
                        ["filename"] = "hello.txt"
                    },
                    new StreamSerializable("\xef\xbb\xbfHello\r\n".ToMemoryStream())
                ),

                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "event",
                        ["length"] = 41L,
                        ["content_type"] = "application/json",
                        ["filename"] = "application.log"
                    },
                    new StreamSerializable("{\"message\":\"hello world\",\"level\":\"error\"}".ToMemoryStream())
                )
            });

        // Act
        using var envelope = await Envelope.DeserializeAsync(input);

        // Assert
        envelope.Should().BeEquivalentTo(expectedEnvelope);
    }

    [Fact]
    public async Task Serialization_EnvelopeWithTwoEmptyItems_Success()
    {
        // Arrange
        using var envelope = new Envelope(
            new Dictionary<string, object> { ["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc" },
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "attachment",
                        ["length"] = 0L
                    },
                    new StreamSerializable(new MemoryStream())
                ),

                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "attachment",
                        ["length"] = 0L
                    },
                    new StreamSerializable(new MemoryStream())
                )
            });

        // Act
        var output = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be(
            "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\",\"sent_at\":\"9999-12-31T23:59:59.9999999+00:00\"}\n" +
            "{\"type\":\"attachment\",\"length\":0}\n" +
            "\n" +
            "{\"type\":\"attachment\",\"length\":0}\n" +
            "\n");
    }

    [Fact]
    public void Serialization_EnvelopeWithTwoEmptyItems_Success_Synchronous()
    {
        // Arrange
        using var envelope = new Envelope(
            new Dictionary<string, object> { ["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc" },
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "attachment",
                        ["length"] = 0L
                    },
                    new StreamSerializable(new MemoryStream())
                ),

                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "attachment",
                        ["length"] = 0L
                    },
                    new StreamSerializable(new MemoryStream())
                )
            });

        // Act
        var output = envelope.SerializeToString(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be(
            "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\",\"sent_at\":\"9999-12-31T23:59:59.9999999+00:00\"}\n" +
            "{\"type\":\"attachment\",\"length\":0}\n" +
            "\n" +
            "{\"type\":\"attachment\",\"length\":0}\n" +
            "\n");
    }

    [Fact]
    public async Task Deserialization_EnvelopeWithTwoEmptyItems_Success()
    {
        // Arrange
        using var input = (
                "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\"}\n" +
                "{\"type\":\"attachment\",\"length\":0}\n" +
                "\n" +
                "{\"type\":\"attachment\",\"length\":0}\n" +
                "\n"
            ).ToMemoryStream();

        using var expectedEnvelope = new Envelope(
            new Dictionary<string, object> { ["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc" },
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "attachment",
                        ["length"] = 0L
                    },
                    new StreamSerializable(new MemoryStream())
                ),

                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "attachment",
                        ["length"] = 0L
                    },
                    new StreamSerializable(new MemoryStream())
                )
            });

        // Act
        using var envelope = await Envelope.DeserializeAsync(input);

        // Assert
        envelope.Should().BeEquivalentTo(expectedEnvelope);
    }

    [Fact]
    public async Task Serialization_EnvelopeWithItemWithoutLength_Success()
    {
        // Arrange
        using var envelope = new Envelope(
            new Dictionary<string, object> { ["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc" },
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object> {["type"] = "attachment"},
                    new StreamSerializable("helloworld".ToMemoryStream())
                )
            });

        // Act
        var output = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be(
            "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\",\"sent_at\":\"9999-12-31T23:59:59.9999999+00:00\"}\n" +
            "{\"type\":\"attachment\",\"length\":10}\n" +
            "helloworld\n");
    }

    [Fact]
    public void Serialization_EnvelopeWithItemWithoutLength_Success_Synchronous()
    {
        // Arrange
        using var envelope = new Envelope(
            new Dictionary<string, object> { ["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc" },
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object> {["type"] = "attachment"},
                    new StreamSerializable("helloworld".ToMemoryStream())
                )
            });

        // Act
        var output = envelope.SerializeToString(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be(
            "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\",\"sent_at\":\"9999-12-31T23:59:59.9999999+00:00\"}\n" +
            "{\"type\":\"attachment\",\"length\":10}\n" +
            "helloworld\n");
    }

    [Fact]
    public async Task Deserialization_EnvelopeWithItemWithoutLength_Success()
    {
        // Arrange
        using var input = (
                "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\"}\n" +
                "{\"type\":\"attachment\"}\n" +
                "helloworld\n"
            ).ToMemoryStream();

        using var expectedEnvelope = new Envelope(
            new Dictionary<string, object> { ["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc" },
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object> {["type"] = "attachment"},
                    new StreamSerializable("helloworld".ToMemoryStream())
                )
            });

        // Act
        using var envelope = await Envelope.DeserializeAsync(input);

        // Assert
        envelope.Should().BeEquivalentTo(expectedEnvelope);
    }

    [Fact]
    public async Task Roundtrip_WithEvent_Success()
    {
        // Arrange
        var ex = new Exception("exception message");
        var timestamp = DateTimeOffset.MaxValue;
        var id = Guid.Parse("4b780f4c-ec03-42a7-8ef8-a41c9d5621f8");
        var @event = new SentryEvent(ex, timestamp, id)
        {
            User = new User { Id = "user-id" },
            Request = new Request { Method = "POST" },
            Contexts = new Contexts
            {
                ["context_key"] = "context_value",
                ["context_key_with_null_value"] = null
            },
            Sdk = new SdkVersion { Name = "SDK-test", Version = "1.0.0" },
            Environment = "environment",
            Level = SentryLevel.Fatal,
            Logger = "logger",
            Message = new SentryMessage
            {
                Message = "message",
                Formatted = "structured_message"
            },
            Modules = { { "module_key", "module_value" } },
            Release = "release",
            Distribution = "distribution",
            SentryExceptions = new[] { new SentryException { Value = "exception_value" } },
            SentryThreads = new[] { new SentryThread { Crashed = true } },
            ServerName = "server_name",
            TransactionName = "transaction",
        };

        @event.SetExtra("extra_key", "extra_value");
        @event.Fingerprint = new[] { "fingerprint" };
        @event.SetTag("tag_key", "tag_value");

        using var envelope = Envelope.FromEvent(@event);

        using var stream = new MemoryStream();

        // Act
        await envelope.SerializeAsync(stream, _testOutputLogger);
        stream.Seek(0, SeekOrigin.Begin);

        using var envelopeRoundtrip = await Envelope.DeserializeAsync(stream);

        // Assert
        envelopeRoundtrip.Should().BeEquivalentTo(envelope);
    }

    [Fact]
    public async Task Null_context_should_not_effect_length_header()
    {
        async Task<Envelope> Roundtrip(SentryEvent sentryEvent)
        {
            using var envelope = Envelope.FromEvent(sentryEvent);

            using var stream = new MemoryStream();
            await envelope.SerializeAsync(stream, _testOutputLogger);
            stream.Seek(0, SeekOrigin.Begin);

            return await Envelope.DeserializeAsync(stream);
        }

        // Arrange
        var eventWithNoNull = new SentryEvent
        {
            Contexts = new()
            {
                ["context_key"] = "context_value"
            },
        };
        var eventWithNull = new SentryEvent(eventId: SentryId.Empty)
        {
            Contexts = new()
            {
                ["context_key"] = "context_value",
                ["context_key_with_null_value"] = null
            },
        };

        using var roundtripWithNoNull = await Roundtrip(eventWithNoNull);
        using var roundtripWithNull = await Roundtrip(eventWithNull);

        var lengthWithNoNull = roundtripWithNoNull.Items[0].TryGetLength()!;
        var lengthWithNull = roundtripWithNull.Items[0].TryGetLength()!;

        // Assert
        Assert.Equal(lengthWithNoNull, lengthWithNull);
    }

    [Fact]
    public async Task Roundtrip_WithEvent_WithAttachment_Success()
    {
        // Arrange
        var @event = new SentryEvent
        {
            Message = "Test",
            Sdk = new SdkVersion { Name = "SDK-test", Version = "1.0.0" }
        };

        using var attachmentStream = new MemoryStream(new byte[] {1, 2, 3});

        var attachment = new Attachment(
            AttachmentType.Default,
            new StreamAttachmentContent(attachmentStream),
            "file.txt",
            null);

        using var envelope = Envelope.FromEvent(@event, null, new[] { attachment });

        using var stream = new MemoryStream();

        // Act
        await envelope.SerializeAsync(stream, _testOutputLogger);
        stream.Seek(0, SeekOrigin.Begin);

        using var envelopeRoundtrip = await Envelope.DeserializeAsync(stream);

        // Assert
        envelopeRoundtrip.Items.Should().HaveCount(2);

        envelopeRoundtrip.Items[0].Payload.Should().BeOfType<JsonSerializable>()
            .Which.Source.Should().BeEquivalentTo(@event);

        envelopeRoundtrip.Items[1].Payload.Should().BeOfType<StreamSerializable>();
    }

    [Fact]
    public async Task Roundtrip_WithEvent_WithSession_Success()
    {
        // Arrange
        var @event = new SentryEvent
        {
            Message = "Test",
            Sdk = new SdkVersion { Name = "SDK-test", Version = "1.0.0" }
        };

        using var attachmentStream = new MemoryStream(new byte[] {1, 2, 3});

        var attachment = new Attachment(
            AttachmentType.Default,
            new StreamAttachmentContent(attachmentStream),
            "file.txt",
            null);

        var sessionUpdate = new Session("foo", "bar", "baz").CreateUpdate(false, DateTimeOffset.Now);

        using var envelope = Envelope.FromEvent(@event, null, new[] { attachment }, sessionUpdate);

        using var stream = new MemoryStream();

        // Act
        await envelope.SerializeAsync(stream, _testOutputLogger);
        stream.Seek(0, SeekOrigin.Begin);

        using var envelopeRoundtrip = await Envelope.DeserializeAsync(stream);

        // Assert
        envelopeRoundtrip.Items.Should().HaveCount(3);

        envelopeRoundtrip.Items[0].Payload.Should().BeOfType<JsonSerializable>()
            .Which.Source.Should().BeEquivalentTo(@event);

        envelopeRoundtrip.Items[1].Payload.Should().BeOfType<StreamSerializable>();

        envelopeRoundtrip.Items[2].Payload.Should().BeOfType<JsonSerializable>()
            .Which.Source.Should().BeEquivalentTo(sessionUpdate);
    }

    [Fact]
    public async Task Roundtrip_WithUserFeedback_Success()
    {
        // Arrange
        var feedback = new UserFeedback(
            SentryId.Create(),
            "Someone Nice",
            "foo@bar.com",
            "Everything is great!");

        using var envelope = Envelope.FromUserFeedback(feedback);

        using var stream = new MemoryStream();

        // Act
        await envelope.SerializeAsync(stream, _testOutputLogger);
        stream.Seek(0, SeekOrigin.Begin);

        using var envelopeRoundtrip = await Envelope.DeserializeAsync(stream);

        // Assert
        envelopeRoundtrip.Should().BeEquivalentTo(envelope);
    }

    [Fact]
    public async Task Roundtrip_WithSession_Success()
    {
        // Arrange
        var sessionUpdate = new Session("foo", "bar", "baz").CreateUpdate(true, DateTimeOffset.Now);

        using var envelope = Envelope.FromSession(sessionUpdate);

        using var stream = new MemoryStream();

        // Act
        await envelope.SerializeAsync(stream, _testOutputLogger);
        stream.Seek(0, SeekOrigin.Begin);

        using var envelopeRoundtrip = await Envelope.DeserializeAsync(stream);

        // Assert
        envelopeRoundtrip.Should().BeEquivalentTo(envelope);
    }

    [Fact]
    public async Task Deserialization_EmptyStream_Throws()
    {
        // Arrange
        using var input = new MemoryStream();

        // Act & assert
        await Assert.ThrowsAnyAsync<Exception>(
            async () => await Envelope.DeserializeAsync(input));
    }

    [Fact]
    public async Task Deserialization_InvalidData_Throws()
    {
        // Arrange
        using var input = new MemoryStream(new byte[1_000_000]); // all 0's

        // Act & assert
        await Assert.ThrowsAnyAsync<Exception>(
            async () => await Envelope.DeserializeAsync(input));
    }

    [Fact]
    public async Task Deserialization_MalformedData_Throws()
    {
        // Arrange
        using var input = "helloworld\n".ToMemoryStream();

        // Act & assert
        await Assert.ThrowsAnyAsync<Exception>(
            async () => await Envelope.DeserializeAsync(input));
    }

    [Fact]
    public void FromEvent_Header_IncludesSdkInformation()
    {
        // Act
        var envelope = Envelope.FromEvent(new SentryEvent());

        // Assert
        envelope.Header.Any(kvp =>
        {
            var (key, value) = kvp;

            return
                key == "sdk" &&
                value is IReadOnlyDictionary<string, string> nested &&
                nested["name"] == SdkVersion.Instance.Name &&
                nested["version"] == SdkVersion.Instance.Version;
        }).Should().BeTrue();
    }

    [Fact]
    public void FromEvent_EmptyAttachmentStream_DoesNotIncludeAttachment()
    {
        // Arrange
        var attachment = new Attachment(
            default,
            new StreamAttachmentContent(Stream.Null),
            "Screenshot.jpg",
            "image/jpg");

        // Act
        var envelope = Envelope.FromEvent(new SentryEvent(), attachments: new List<Attachment> { attachment });

        // Assert
        envelope.Items.Should().HaveCount(1);
    }

    [Fact]
    public void FromEvent_EmptyAttachmentStream_DisposesStream()
    {
        // Arrange
        var path = Path.GetTempFileName();
        using var stream = File.OpenRead(path);
        var attachment = new Attachment(
            default,
            new StreamAttachmentContent(stream),
            "Screenshot.jpg",
            "image/jpg");

        // Act
        _ = Envelope.FromEvent(new SentryEvent(), attachments: new List<Attachment> { attachment });

        // Assert
        Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
    }

    [Fact]
    public async Task Serialization_RoundTrip_ReplacesSentAtHeader()
    {
        // Arrange
        using var envelope = new Envelope(
            new Dictionary<string, object> { ["event_id"] = "12c2d058d58442709aa2eca08bf20986" },
            Array.Empty<EnvelopeItem>());

        // Act
        _fakeClock.SetUtcNow(DateTimeOffset.MinValue);
        var serialized = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(serialized);
        await writer.FlushAsync();
        stream.Seek(0, SeekOrigin.Begin);
        var deserialized = await Envelope.DeserializeAsync(stream);

        _fakeClock.SetUtcNow(DateTimeOffset.MaxValue);
        var output = await deserialized.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be(
            "{\"event_id\":\"12c2d058d58442709aa2eca08bf20986\",\"sent_at\":\"9999-12-31T23:59:59.9999999+00:00\"}\n");
    }
}
