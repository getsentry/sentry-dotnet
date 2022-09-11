using FluentAssertions.Primitives;

namespace Sentry.Testing;

// Reference: https://fluentassertions.com/extensibility/

// This code ensures that when we compare Envelope objects for equivalency, that we
// ignore the `sent_at` header in the envelope, and the `length` header in each item within the envelope.
// TODO: There's probably a better implementation using FA's "step by step" assertions.
// https://fluentassertions.com/extensibility/#equivalency-assertion-step-by-step

public static class FluentAssertionExtensions
{
    public static EnvelopeAssertions Should(this Envelope instance) => new(instance);
    public static EnvelopeItemAssertions Should(this EnvelopeItem instance) => new(instance);
}

public class EnvelopeAssertions : ReferenceTypeAssertions<Envelope, EnvelopeAssertions>
{
    private const string SentAtKey = "sent_at";

    public EnvelopeAssertions(Envelope instance) : base(instance)
    {
    }

    protected override string Identifier => "envelope";

    public AndConstraint<EnvelopeAssertions> BeEquivalentTo(Envelope expectation)
    {
        var subjectHasKey = Subject.Header.ContainsKey(SentAtKey);
        var expectationHasKey = expectation.Header.ContainsKey(SentAtKey);
        if (subjectHasKey == expectationHasKey)
        {
            // We can check the header directly
            Subject.Header.Should().BeEquivalentTo(expectation.Header);
        }
        else
        {
            // Check the header separately so we can exclude sent_at
            Subject.Header
                .Where(x => x.Key != SentAtKey)
                .Should().BeEquivalentTo(expectation.Header);
        }

        // Check the items individually so we can apply our custom assertion to each item
        Subject.Items.Should().HaveSameCount(expectation.Items);
        for (int i = 0; i < Subject.Items.Count; i++)
        {
            Subject.Items[i].Should().BeEquivalentTo(expectation.Items[i]);
        }

        return new(this);
    }
}

public class EnvelopeItemAssertions : ReferenceTypeAssertions<EnvelopeItem, EnvelopeItemAssertions>
{
    private const string LengthKey = "length";

    public EnvelopeItemAssertions(EnvelopeItem instance) : base(instance)
    {
    }

    protected override string Identifier => "envelope item";

    public AndConstraint<EnvelopeItemAssertions> BeEquivalentTo(EnvelopeItem expectation)
    {
        var subjectHasKey = Subject.Header.ContainsKey(LengthKey);
        var expectationHasKey = expectation.Header.ContainsKey(LengthKey);
        if (subjectHasKey == expectationHasKey)
        {
            // We can check the entire object directly
            AssertionExtensions.Should(Subject).BeEquivalentTo(expectation);
        }
        else
        {
            // Check the header separately so we can exclude length
            Subject.Header
                .Where(x => x.Key != LengthKey)
                .Should().BeEquivalentTo(expectation.Header);

            // Check the rest of the item, excluding the header
            AssertionExtensions.Should(Subject).BeEquivalentTo(expectation,
                o => o.Excluding(item => item.Header));
        }

        return new(this);
    }
}
