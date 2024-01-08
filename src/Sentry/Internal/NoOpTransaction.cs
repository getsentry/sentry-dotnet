using Sentry.Protocol;

namespace Sentry.Internal;

/// <summary>
/// Transaction class to use when we can't return null but a request to create a transaction couldn't be completed.
/// </summary>
internal class NoOpTransaction : NoOpSpan, ITransactionTracer
{
    public new static ITransactionTracer Instance { get; } = new NoOpTransaction();

    private NoOpTransaction()
    {
    }

    public SdkVersion Sdk => SdkVersion.Instance;

    public string Name
    {
        get => string.Empty;
        set { }
    }

    public bool? IsParentSampled
    {
        get => default;
        set { }
    }

    public TransactionNameSource NameSource => TransactionNameSource.Custom;

    public string? Distribution
    {
        get => string.Empty;
        set { }
    }

    public SentryLevel? Level
    {
        get => default;
        set { }
    }

    public Request Request{
        get => new();
        set { }
    }

    public Contexts Contexts{
        get => new();
        set { }
    }

    public SentryUser User
    {
        get => new();
        set { }
    }

    public string? Platform
    {
        get => default;
        set { }
    }

    public string? Release
    {
        get => default;
        set { }
    }

    public string? Environment
    {
        get => default;
        set { }
    }

    public string? TransactionName
    {
        get => default;
        set { }
    }

    public IReadOnlyList<string> Fingerprint
    {
        get => ImmutableList<string>.Empty;
        set { }
    }

    public IReadOnlyCollection<ISpan> Spans => ImmutableList<ISpan>.Empty;

    public IReadOnlyCollection<Breadcrumb> Breadcrumbs => ImmutableList<Breadcrumb>.Empty;

    public ISpan? GetLastActiveSpan() => default;

    public void AddBreadcrumb(Breadcrumb breadcrumb) { }
}
