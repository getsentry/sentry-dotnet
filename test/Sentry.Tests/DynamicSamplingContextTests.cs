namespace Sentry.Tests;

public class DynamicSamplingContextTests
{
    [Fact]
    public void EmptyContext()
    {
        var dsc = DynamicSamplingContext.Empty;

        Assert.True(dsc.IsEmpty);
    }

    [Fact]
    public void CreateFromBaggage_TraceId_Missing()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "1.0"}
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        Assert.Null(dsc);
    }

    [Fact]
    public void CreateFromBaggage_TraceId_EmptyGuid()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "00000000000000000000000000000000"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "1.0"}
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        Assert.Null(dsc);
    }

    [Fact]
    public void CreateFromBaggage_TraceId_Invalid()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "not-a-guid"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "1.0"}
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        Assert.Null(dsc);
    }

    [Fact]
    public void CreateFromBaggage_PublicKey_Missing()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-sample_rate", "1.0"}
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        Assert.Null(dsc);
    }

    [Fact]
    public void CreateFromBaggage_PublicKey_Blank()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", " "},
            {"sentry-sample_rate", "1.0"}
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        Assert.Null(dsc);
    }

    [Fact]
    public void CreateFromBaggage_SampleRate_Missing()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"}
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        Assert.Null(dsc);
    }

    [Fact]
    public void CreateFromBaggage_SampleRate_Invalid()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "not-a-number"}
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        Assert.Null(dsc);
    }

    [Fact]
    public void CreateFromBaggage_SampleRate_TooLow()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "-0.1"}
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        Assert.Null(dsc);
    }

    [Fact]
    public void CreateFromBaggage_SampleRate_TooHigh()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "1.1"}
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        Assert.Null(dsc);
    }

    [Fact]
    public void CreateFromBaggage_SampleRand_Invalid()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "1.0"},
            {"sentry-sample_rand", "not-a-number"},
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        Assert.Null(dsc);
    }

    [Fact]
    public void CreateFromBaggage_SampleRand_TooLow()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "1.0"},
            {"sentry-sample_rand", "-0.1"}
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        Assert.Null(dsc);
    }

    [Fact]
    public void CreateFromBaggage_SampleRand_TooHigh()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "1.0"},
            {"sentry-sample_rand", "1.0"} // Must be less than 1
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        Assert.Null(dsc);
    }

    [Fact]
    public void CreateFromBaggage_NotSampledNoSampleRand_GeneratesSampleRand()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "0.5"}
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        using var scope = new AssertionScope();
        Assert.NotNull(dsc);
        var sampleRandItem = Assert.Contains("sample_rand", dsc.Items);
        var sampleRand = double.Parse(sampleRandItem, NumberStyles.Float, CultureInfo.InvariantCulture);
        Assert.True(sampleRand >= 0.0);
        Assert.True(sampleRand < 1.0);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void CreateFromBaggage_SampledNoSampleRand_GeneratesConsistentSampleRand(string sampled)
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "0.5"},
            {"sentry-sampled", sampled},
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        using var scope = new AssertionScope();
        Assert.NotNull(dsc);
        var sampleRandItem = Assert.Contains("sample_rand", dsc.Items);
        var sampleRand = double.Parse(sampleRandItem, NumberStyles.Float, CultureInfo.InvariantCulture);
        if (sampled == "true")
        {
            Assert.True(sampleRand >= 0.0);
            Assert.True(sampleRand < 0.5);
        }
        else
        {
            Assert.True(sampleRand >= 0.5);
            Assert.True(sampleRand < 1.0);
        }
    }

    [Fact]
    public void CreateFromBaggage_Sampled_MalFormed()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "1.0"},
            {"sentry-sampled", "foo"},
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        Assert.Null(dsc);
    }

    [Fact]
    public void CreateFromBaggage_Valid_Minimum()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "1.0"}
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        Assert.NotNull(dsc);
        Assert.Equal(4, dsc.Items.Count);
        Assert.Equal("43365712692146d08ee11a729dfbcaca", Assert.Contains("trace_id", dsc.Items));
        Assert.Equal("d4d82fc1c2c4032a83f3a29aa3a3aff", Assert.Contains("public_key", dsc.Items));
        Assert.Equal("1.0", Assert.Contains("sample_rate", dsc.Items));
        Assert.Contains("sample_rand", dsc.Items);
    }

    [Fact]
    public void CreateFromBaggage_Valid_Complete()
    {
        var baggage = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sampled", "true"},
            {"sentry-sample_rate", "1.0"},
            {"sentry-sample_rand", "0.1234"},
            {"sentry-release", "test@1.0.0+abc"},
            {"sentry-environment", "production"},
            {"sentry-user_segment", "Group B"},
            {"sentry-transaction", "GET /person/{id}"}
        });

        var dsc = baggage.CreateDynamicSamplingContext();

        Assert.NotNull(dsc);
        Assert.Equal(baggage.Members.Count, dsc.Items.Count);
        Assert.Equal("43365712692146d08ee11a729dfbcaca", Assert.Contains("trace_id", dsc.Items));
        Assert.Equal("d4d82fc1c2c4032a83f3a29aa3a3aff", Assert.Contains("public_key", dsc.Items));
        Assert.Equal("true", Assert.Contains("sampled", dsc.Items));
        Assert.Equal("1.0", Assert.Contains("sample_rate", dsc.Items));
        Assert.Equal("0.1234", Assert.Contains("sample_rand", dsc.Items));
        Assert.Equal("test@1.0.0+abc", Assert.Contains("release", dsc.Items));
        Assert.Equal("production", Assert.Contains("environment", dsc.Items));
        Assert.Equal("Group B", Assert.Contains("user_segment", dsc.Items));
        Assert.Equal("GET /person/{id}", Assert.Contains("transaction", dsc.Items));
    }

    [Fact]
    public void ToBaggageHeader()
    {
        var original = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"sentry-trace_id", "43365712692146d08ee11a729dfbcaca"},
            {"sentry-public_key", "d4d82fc1c2c4032a83f3a29aa3a3aff"},
            {"sentry-sample_rate", "1.0"},
            {"sentry-sample_rand", "0.1234"},
            {"sentry-release", "test@1.0.0+abc"},
            {"sentry-environment", "production"},
            {"sentry-user_segment", "Group B"},
            {"sentry-transaction", "GET /person/{id}"}
        });

        var dsc = original.CreateDynamicSamplingContext();

        var result = dsc?.ToBaggageHeader();

        Assert.NotNull(dsc);
        Assert.Equal(original.Members, result.Members);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [InlineData(null)]
    public void CreateFromTransaction(bool? isSampled)
    {
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            Release = "foo@2.4.5",
            Environment = "staging"
        };

        var hub = Substitute.For<IHub>();
        var ctx = Substitute.For<ITransactionContext>();

        var traceId = SentryId.Create();
        ctx.TraceId.Returns(traceId);

        var transaction = new TransactionTracer(hub, ctx)
        {
            Name = "GET /person/{id}",
            NameSource = TransactionNameSource.Route,
            IsSampled = isSampled,
            SampleRate = 0.5,
            SampleRand = (isSampled ?? true) ? 0.4000 : 0.6000, // Lower than the sample rate means sampled == true
            User =
            {
            },
        };

        var dsc = transaction.CreateDynamicSamplingContext(options);

        Assert.NotNull(dsc);
        Assert.Equal(isSampled.HasValue ? 8 : 7, dsc.Items.Count);
        Assert.Equal(traceId.ToString(), Assert.Contains("trace_id", dsc.Items));
        Assert.Equal("d4d82fc1c2c4032a83f3a29aa3a3aff", Assert.Contains("public_key", dsc.Items));
        if (transaction.IsSampled is { } sampled)
        {
            Assert.Equal(sampled ? "true" : "false", Assert.Contains("sampled", dsc.Items));
        }
        else
        {
            Assert.DoesNotContain("sampled", dsc.Items);
        }
        Assert.Equal("0.5", Assert.Contains("sample_rate", dsc.Items));
        Assert.Equal((isSampled ?? true) ? "0.4000" : "0.6000", Assert.Contains("sample_rand", dsc.Items));
        Assert.Equal("foo@2.4.5", Assert.Contains("release", dsc.Items));
        Assert.Equal("staging", Assert.Contains("environment", dsc.Items));
        Assert.Equal("GET /person/{id}", Assert.Contains("transaction", dsc.Items));
    }

    [Fact]
    public void CreateFromPropagationContext_Valid_Complete()
    {
        var options = new SentryOptions { Dsn = "https://a@sentry.io/1", Release = "test-release", Environment = "test-environment" };
        var propagationContext = new SentryPropagationContext(
            SentryId.Parse("43365712692146d08ee11a729dfbcaca"), SpanId.Parse("1234"));

        var dsc = propagationContext.CreateDynamicSamplingContext(options);

        Assert.NotNull(dsc);
        Assert.Equal("43365712692146d08ee11a729dfbcaca", Assert.Contains("trace_id", dsc.Items));
        Assert.Equal("a", Assert.Contains("public_key", dsc.Items));
        Assert.Equal("test-release", Assert.Contains("release", dsc.Items));
        Assert.Equal("test-environment", Assert.Contains("environment", dsc.Items));
    }
}
