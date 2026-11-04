using Sentry.Infrastructure;

namespace Sentry;

/// <summary>
/// How a category of key-value data is collected.
/// </summary>
[Experimental(DiagnosticId.ExperimentalFeature)]
public enum KeyValueCollectionMode
{
    /// <summary>
    /// Neither keys nor values are collected.
    /// </summary>
    Off,

    /// <summary>
    /// All key names are collected. Values are sent as-is, except for keys matching the built-in sensitive terms
    /// or any additional terms supplied with the behavior, whose values are replaced with <c>[Filtered]</c>.
    /// </summary>
    DenyList,

    /// <summary>
    /// All key names are collected, but only values of keys matching the supplied terms are sent as-is. Values of
    /// keys matching the built-in sensitive terms are replaced with <c>[Filtered]</c> even when they match a
    /// supplied term.
    /// </summary>
    AllowList
}

/// <summary>
/// Controls how a category of automatically collected key-value data - HTTP headers, cookies, URL query
/// parameters or stack frame variables - is collected.
/// </summary>
/// <remarks>
/// Terms are matched against key names as case-insensitive substrings. The built-in sensitive terms always apply,
/// so additional deny terms extend that list rather than replacing it.
/// <para>
/// In configuration this binds from <c>Mode</c> and <c>Terms</c> keys, for example
/// <c>Sentry:DataCollection:Cookies:Mode=Off</c>. The <c>bool</c> shorthand available in code has no configuration
/// equivalent, so a scalar such as <c>Sentry:DataCollection:Cookies=false</c> is ignored.
/// </para>
/// <para>This API is experimental, and it may change in the future.</para>
/// </remarks>
[Experimental(DiagnosticId.ExperimentalFeature)]
public sealed class KeyValueCollectionBehavior
{
    private IReadOnlyList<string> _terms = [];

    /// <summary>
    /// Creates a behavior that collects all key names and filters values of sensitive keys, equivalent to
    /// <see cref="DenyList"/>. Set <see cref="Mode"/> and <see cref="Terms"/> to configure it, or use
    /// <see cref="Off"/>, <see cref="DenyList"/> and <see cref="AllowList"/>.
    /// </summary>
    public KeyValueCollectionBehavior()
    {
    }

    private KeyValueCollectionBehavior(KeyValueCollectionMode mode, IReadOnlyList<string>? terms)
    {
        Mode = mode;
        Terms = terms!;
    }

    /// <summary>
    /// How this category is collected.
    /// </summary>
    public KeyValueCollectionMode Mode { get; set; } = KeyValueCollectionMode.DenyList;

    /// <summary>
    /// Additional deny terms in <see cref="KeyValueCollectionMode.DenyList"/> mode, or the allowed key terms in
    /// <see cref="KeyValueCollectionMode.AllowList"/> mode. Never null, and never contains a null or empty term,
    /// which would otherwise match every key. Empty in <see cref="KeyValueCollectionMode.Off"/> mode, or when no
    /// terms were supplied.
    /// </summary>
    public IReadOnlyList<string> Terms
    {
        get => _terms;
        set => _terms = value?.Where(term => !string.IsNullOrEmpty(term)).ToArray() ?? [];
    }

    /// <summary>
    /// Collect neither keys nor values.
    /// </summary>
    public static KeyValueCollectionBehavior Off => new(KeyValueCollectionMode.Off, []);

    /// <summary>
    /// Collect all key names, replacing values of sensitive keys with <c>[Filtered]</c>. This is the default
    /// behavior for every key-value category.
    /// </summary>
    /// <param name="additionalTerms">
    /// Key name terms to filter in addition to the built-in sensitive terms. To restore the header filtering that
    /// <see cref="SentryOptions.SendDefaultPii"/> applied when disabled, pass
    /// <c>"forwarded"</c>, <c>"-ip"</c>, <c>"remote-"</c>, <c>"via"</c> and <c>"-user"</c>.
    /// </param>
    public static KeyValueCollectionBehavior DenyList(params string[] additionalTerms) =>
        new(KeyValueCollectionMode.DenyList, additionalTerms);

    /// <summary>
    /// Collect all key names, but send values only for keys matching <paramref name="terms"/>. Values of keys
    /// matching the built-in sensitive terms are still replaced with <c>[Filtered]</c>.
    /// </summary>
    /// <param name="terms">Key name terms whose values are sent.</param>
    public static KeyValueCollectionBehavior AllowList(params string[] terms) =>
        new(KeyValueCollectionMode.AllowList, terms);

    /// <summary>
    /// Converts a boolean to a behavior, so categories can be switched on and off without naming a mode.
    /// <c>true</c> is equivalent to <see cref="DenyList"/> and <c>false</c> to <see cref="Off"/>.
    /// </summary>
    /// <param name="collect">Whether to collect the category.</param>
    public static implicit operator KeyValueCollectionBehavior(bool collect) => collect ? DenyList() : Off;
}
