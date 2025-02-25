namespace Sentry.Tests.Protocol.Envelopes;

public class EnvelopeTests
{
    // Tests driven by examples from docs:
    // https://develop.sentry.dev/sdk/envelopes/#full-examples

    private readonly IDiagnosticLogger _testOutputLogger;
    private readonly MockClock _fakeClock;

    private const string TestAttachmentText = "\xef\xbb\xbfHello\r\n";

    public EnvelopeTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
        _fakeClock = new MockClock(DateTimeOffset.MaxValue);
    }

    [Fact]
    public void TryGetEventId()
    {
        using var envelope = new Envelope(
            new Dictionary<string, object> { ["event_id"] = "12c2d058d58442709aa2eca08bf20986" },
            Array.Empty<EnvelopeItem>());
        var logger = new InMemoryDiagnosticLogger();

        var id = envelope.TryGetEventId(logger);

        Assert.Equal("12c2d058d58442709aa2eca08bf20986", id?.ToString());
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void TryGetEventId_none()
    {
        using var envelope = new Envelope(
            new Dictionary<string, object>(),
            Array.Empty<EnvelopeItem>());
        var logger = new InMemoryDiagnosticLogger();

        var id = envelope.TryGetEventId(logger);

        Assert.Null(id);
        Assert.Empty(logger.Entries);
    }

    [Theory]
    [InlineData(null, false, "Header event_id is null")]
    [InlineData(10, false, "Header event_id has incorrect type: System.Int32")]
    [InlineData("not-guid", false, "Header event_id is not a GUID: not-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000", true, "Envelope contains an empty event_id header")]
    public void TryGetEventId_Errors(object value, bool expectEmpty, string message)
    {
        // Arrange
        using var envelope = new Envelope(
            new Dictionary<string, object> { ["event_id"] = value },
            Array.Empty<EnvelopeItem>());

        var logger = new InMemoryDiagnosticLogger();

        var id = envelope.TryGetEventId(logger);
        Assert.Equal(message, logger.Entries.Single().Message);
        if (expectEmpty)
        {
            Assert.Equal(SentryId.Empty, id);
        }
        else
        {
            Assert.Null(id);
        }
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
        output.Should().Be("""
            {"event_id":"12c2d058d58442709aa2eca08bf20986","sent_at":"9999-12-31T23:59:59.9999999+00:00"}

            """);
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
        output.Should().Be("""
            {"event_id":"12c2d058d58442709aa2eca08bf20986","sent_at":"9999-12-31T23:59:59.9999999+00:00"}

            """);
    }

    [Fact]
    public async Task Deserialization_EnvelopeWithoutItems_Success()
    {
        // Arrange
        using var input = """
            {"event_id":"12c2d058d58442709aa2eca08bf20986"}

            """.ToMemoryStream();

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
                    new StreamSerializable("""
                        {"started": "2020-02-07T14:16:00Z","attrs":{"release":"sentry-test@1.0.0"}}
                        """.ToMemoryStream())
                )
            });

        // Act
        var output = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be("""
            {"sent_at":"9999-12-31T23:59:59.9999999+00:00"}
            {"type":"session","length":75}
            {"started": "2020-02-07T14:16:00Z","attrs":{"release":"sentry-test@1.0.0"}}

            """);
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
                    new StreamSerializable("""
                        {"started": "2020-02-07T14:16:00Z","attrs":{"release":"sentry-test@1.0.0"}}
                        """.ToMemoryStream())
                )
            });

        // Act
        var output = envelope.SerializeToString(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be("""
            {"sent_at":"9999-12-31T23:59:59.9999999+00:00"}
            {"type":"session","length":75}
            {"started": "2020-02-07T14:16:00Z","attrs":{"release":"sentry-test@1.0.0"}}

            """);
    }

    [Fact]
    public async Task Deserialization_EnvelopeWithoutHeader_Success()
    {
        // Arrange
        using var input = """
                {}
                {"type":"fake","length":75}
                {"started": "2020-02-07T14:16:00Z","attrs":{"release":"sentry-test@1.0.0"}}

                """.ToMemoryStream();

        using var expectedEnvelope = new Envelope(
            new Dictionary<string, object>(),
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object>{["type"] = "fake"},
                    new StreamSerializable("""
                        {"started": "2020-02-07T14:16:00Z","attrs":{"release":"sentry-test@1.0.0"}}
                        """.ToMemoryStream())
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
                    new StreamSerializable(TestAttachmentText.ToMemoryStream())
                ),

                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "event",
                        ["length"] = 41,
                        ["content_type"] = "application/json",
                        ["filename"] = "application.log"
                    },
                    new StreamSerializable("""
                        {"message":"hello world","level":"error"}
                        """.ToMemoryStream())
                )
            });

        // Act
        var output = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be($$"""
            {"event_id":"9ec79c33ec9942ab8353589fcb2e04dc","dsn":"https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42","sent_at":"9999-12-31T23:59:59.9999999+00:00"}
            {"type":"attachment","length":13,"content_type":"text/plain","filename":"hello.txt"}
            {{TestAttachmentText}}
            {"type":"event","length":41,"content_type":"application/json","filename":"application.log"}
            {"message":"hello world","level":"error"}

            """);
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
                    new StreamSerializable(TestAttachmentText.ToMemoryStream())
                ),

                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "event",
                        ["length"] = 41,
                        ["content_type"] = "application/json",
                        ["filename"] = "application.log"
                    },
                    new StreamSerializable("""{"message":"hello world","level":"error"}""".ToMemoryStream())
                )
            });

        // Act
        var output = envelope.SerializeToString(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be($$"""
            {"event_id":"9ec79c33ec9942ab8353589fcb2e04dc","dsn":"https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42","sent_at":"9999-12-31T23:59:59.9999999+00:00"}
            {"type":"attachment","length":13,"content_type":"text/plain","filename":"hello.txt"}
            {{TestAttachmentText}}
            {"type":"event","length":41,"content_type":"application/json","filename":"application.log"}
            {"message":"hello world","level":"error"}

            """);
    }

    [Fact]
    public async Task Deserialization_EnvelopeWithTwoItems_Success()
    {
        using var input = $$"""
            {"event_id":"9ec79c33ec9942ab8353589fcb2e04dc","dsn":"https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42"}
            {"type":"attachment","length":13,"content_type":"text/plain","filename":"hello.txt"}
            {{TestAttachmentText}}
            {"type":"event","length":41,"content_type":"application/json","filename":"application.log"}
            {"message":"hello world","level":"error"}

            """.ToMemoryStream();

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
                        ["content_type"] = "text/plain",
                        ["filename"] = "hello.txt"
                    },
                    new StreamSerializable(TestAttachmentText.ToMemoryStream())
                ),

                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "event",
                        ["content_type"] = "application/json",
                        ["filename"] = "application.log"
                    },
                    new StreamSerializable("""{"message":"hello world","level":"error"}""".ToMemoryStream())
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
        output.Should().Be("""
            {"event_id":"9ec79c33ec9942ab8353589fcb2e04dc","sent_at":"9999-12-31T23:59:59.9999999+00:00"}
            {"type":"attachment","length":0}

            {"type":"attachment","length":0}


            """);
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
        output.Should().Be("""
            {"event_id":"9ec79c33ec9942ab8353589fcb2e04dc","sent_at":"9999-12-31T23:59:59.9999999+00:00"}
            {"type":"attachment","length":0}

            {"type":"attachment","length":0}


            """);
    }

    [Fact]
    public async Task Deserialization_EnvelopeWithTwoEmptyItems_Success()
    {
        // Arrange
        using var input = """
            {"event_id":"9ec79c33ec9942ab8353589fcb2e04dc"}
            {"type":"attachment","length":0}

            {"type":"attachment","length":0}


            """.ToMemoryStream();

        using var expectedEnvelope = new Envelope(
            new Dictionary<string, object> { ["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc" },
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "attachment"
                    },
                    new StreamSerializable(new MemoryStream())
                ),

                new EnvelopeItem(
                    new Dictionary<string, object>
                    {
                        ["type"] = "attachment"
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
        output.Should().Be("""
            {"event_id":"9ec79c33ec9942ab8353589fcb2e04dc","sent_at":"9999-12-31T23:59:59.9999999+00:00"}
            {"type":"attachment","length":10}
            helloworld

            """);
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
        output.Should().Be("""
            {"event_id":"9ec79c33ec9942ab8353589fcb2e04dc","sent_at":"9999-12-31T23:59:59.9999999+00:00"}
            {"type":"attachment","length":10}
            helloworld

            """);
    }

    private class ThrowingSerializable : ISentryJsonSerializable
    {
        public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger logger)
        {
            throw new InvalidOperationException("test");
        }
    }

    [Fact]
    public void Serialization_EnvelopeWithThrowingItem_DoesntThrow()
    {
        // Arrange
        using var envelope = new Envelope(
            new Dictionary<string, object> { ["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc" },
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object> {["type"] = "attachment"},
                    AsyncJsonSerializable.CreateFrom(Task.Run(() => new ThrowingSerializable()))
                )
            });

        // Act
        var output = envelope.SerializeToString(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be("""
            {"event_id":"9ec79c33ec9942ab8353589fcb2e04dc","sent_at":"9999-12-31T23:59:59.9999999+00:00"}

            """);
    }

    [Fact]
    public async Task AsyncSerialization_EnvelopeWithThrowingItem_DoesntThrow()
    {
        // Arrange
        using var envelope = new Envelope(
            new Dictionary<string, object> { ["event_id"] = "9ec79c33ec9942ab8353589fcb2e04dc" },
            new[]
            {
                new EnvelopeItem(
                    new Dictionary<string, object> {["type"] = "attachment"},
                    AsyncJsonSerializable.CreateFrom(Task.Run(() => new ThrowingSerializable()))
                )
            });

        // Act
        var output = await envelope.SerializeToStringAsync(_testOutputLogger, _fakeClock);

        // Assert
        output.Should().Be("""
            {"event_id":"9ec79c33ec9942ab8353589fcb2e04dc","sent_at":"9999-12-31T23:59:59.9999999+00:00"}

            """);
    }

    [Fact]
    public async Task Deserialization_EnvelopeWithItemWithoutLength_Success()
    {
        // Arrange
        using var input = """
            {"event_id":"9ec79c33ec9942ab8353589fcb2e04dc"}
            {"type":"attachment"}
            helloworld

            """.ToMemoryStream();

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
            User = new SentryUser { Id = "user-id" },
            Request = new SentryRequest { Method = "POST" },
            Contexts = new SentryContexts
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
        var sentryId = SentryId.Create();
        var timestamp = DateTimeOffset.Now;
        var eventWithNoNull = new SentryEvent(eventId: sentryId, timestamp: timestamp)
        {
            Contexts = new()
            {
                ["context_key"] = "context_value"
            },
        };
        var eventWithNull = new SentryEvent(eventId: sentryId, timestamp: timestamp)
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

        using var attachmentStream = new MemoryStream(new byte[] { 1, 2, 3 });

        var attachment = new SentryAttachment(
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
        envelopeRoundtrip.Items[1].TryGetLength().Should().BeNull();
        envelopeRoundtrip.Items[1].TryGetOrRecalculateLength().Should().Be(attachment.Content.GetStream().Length);
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

        using var attachmentStream = new MemoryStream(new byte[] { 1, 2, 3 });

        var attachment = new SentryAttachment(
            AttachmentType.Default,
            new StreamAttachmentContent(attachmentStream),
            "file.txt",
            null);

        var sessionUpdate = new SentrySession("foo", "bar", "baz").CreateUpdate(false, DateTimeOffset.Now);

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
    public async Task Roundtrip_WithFeedback_Success()
    {
        // Arrange
        var feedback = new SentryFeedback(
            "Everything is great!",
            "foo@bar.com",
            "Someone Nice",
            "fake-replay-id",
            "https://www.example.com",
            SentryId.Create()
        );
        var evt = new SentryEvent { Level = SentryLevel.Info,
            Contexts =
            {
                Feedback = feedback
            }
        };

        using var envelope = Envelope.FromFeedback(evt);

        using var stream = new MemoryStream();

        // Act
        await envelope.SerializeAsync(stream, _testOutputLogger);
        stream.Seek(0, SeekOrigin.Begin);

        using var envelopeRoundtrip = await Envelope.DeserializeAsync(stream);

        // Assert
        envelopeRoundtrip.Should().BeEquivalentTo(envelope);
    }

    [Fact]
    public void FromFeedback_NoFeedbackContext_Throws()
    {
        // Arrange
        var evt = new SentryEvent { Level = SentryLevel.Info };

        // Act
        Action act = () => Envelope.FromFeedback(evt);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Unable to create envelope - the event does not contain any feedback.");
    }

    [Fact]
    public void FromFeedback_MultipleAttachments_LogsWarning()
    {
        // Arrange
        var feedback = new SentryFeedback(
            "Everything is great!",
            "foo@bar.com",
            "Someone Nice",
            "fake-replay-id",
            "https://www.example.com",
            SentryId.Create()
        );
        var evt = new SentryEvent { Level = SentryLevel.Info,
            Contexts =
            {
                Feedback = feedback
            }
        };
        var logger = Substitute.For<IDiagnosticLogger>();
        logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);

        List<SentryAttachment> attachments = [
            AttachmentHelper.FakeAttachment("file1.txt"), AttachmentHelper.FakeAttachment("file2.txt")
        ];

        // Act
        using var envelope = Envelope.FromFeedback(evt, logger, attachments);

        // Assert
        logger.Received(1).Log(
            SentryLevel.Warning,
            Arg.Is<string>(m => m.Contains("Feedback can only contain one attachment")),
            null,
            Arg.Any<object[]>());
        envelope.Items.Should().ContainSingle(item => item.TryGetType() == EnvelopeItem.TypeValueAttachment);
    }

    [Fact]
    public async Task Roundtrip_WithSession_Success()
    {
        // Arrange
        var sessionUpdate = new SentrySession("foo", "bar", "baz").CreateUpdate(true, DateTimeOffset.Now);

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
        var attachment = new SentryAttachment(
            default,
            new StreamAttachmentContent(Stream.Null),
            "Screenshot.jpg",
            "image/jpg");

        // Act
        var envelope = Envelope.FromEvent(new SentryEvent(), attachments: new List<SentryAttachment> { attachment });

        // Assert
        envelope.Items.Should().HaveCount(1);
    }

    [Fact]
    public void FromEvent_EmptyAttachmentStream_DisposesStream()
    {
        // Arrange
        var path = Path.GetTempFileName();
        using var stream = File.OpenRead(path);
        var attachment = new SentryAttachment(
            default,
            new StreamAttachmentContent(stream),
            "Screenshot.jpg",
            "image/jpg");

        // Act
        _ = Envelope.FromEvent(new SentryEvent(), attachments: new List<SentryAttachment> { attachment });

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
        output.Should().Be("""
            {"event_id":"12c2d058d58442709aa2eca08bf20986","sent_at":"9999-12-31T23:59:59.9999999+00:00"}

            """);
    }

    [Fact]
    public async Task Serialization_RoundTrip_RecalculatesLengthHeader()
    {
        // See https://github.com/getsentry/sentry-dotnet/issues/1956

        // Arrange
        var dto = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(1));
        var evt = new SentryEvent(timestamp: DateTimeOffset.MaxValue);
        evt.Sdk = new SdkVersion
        {
            Name = "test",
            Version = "0.0.0"
        };
        evt.SetExtra("foo", dto);
        var envelope = Envelope.FromEvent(evt);

        // Act
        var serialized1 = await envelope.SerializeToStringAsync(_testOutputLogger);

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(serialized1);
        await writer.FlushAsync();
        stream.Seek(0, SeekOrigin.Begin);
        var deserialized = await Envelope.DeserializeAsync(stream);
        var serialized2 = await deserialized.SerializeToStringAsync(_testOutputLogger);

        // Assert

        Assert.False(deserialized.Items[0].Header.ContainsKey("length"),
            "The length header should not have been deserialized.");

        // these are the actual lengths
        var length1 = serialized1.Split('\n', StringSplitOptions.RemoveEmptyEntries).Last().Length;
        var length2 = serialized2.Split('\n', StringSplitOptions.RemoveEmptyEntries).Last().Length;

        // the header should contain those lengths
        Assert.Contains($$"""{"type":"event","length":{{length1}}}""", serialized1);
        Assert.Contains($$"""{"type":"event","length":{{length2}}}""", serialized2);

        // this is the main difference between them
        Assert.Contains("""{"foo":"2020-01-01T00:00:00+01:00"}""", serialized1);
        Assert.Contains("""{"foo":"2020-01-01T00:00:00\u002B01:00"}""", serialized2);
    }
}
