namespace Sentry.Internal.DataCollection;

/// <summary>
/// How a key-value collection (HTTP headers, cookies, URL query parameters) should be collected, per the
/// DataCollection spec: https://develop.sentry.dev/sdk/foundations/client/data-collection/
/// </summary>
internal enum KeyValueFilterMode
{
    /// <summary>Neither keys nor values are collected.</summary>
    Off,

    /// <summary>All keys are collected; values of sensitive keys are replaced with "[Filtered]".</summary>
    DenyList,

    /// <summary>All keys are collected; only values of allow-listed, non-sensitive keys are kept.</summary>
    AllowList
}

/// <summary>
/// A <see cref="KeyValueFilterMode"/> plus the terms that parameterize it: extra deny terms in
/// <see cref="KeyValueFilterMode.DenyList"/> mode, allowed key terms in <see cref="KeyValueFilterMode.AllowList"/>
/// mode. Terms match key names as case-insensitive substrings. Internal precursor to the public behavior type that
/// ships with the DataCollection option.
/// </summary>
internal readonly struct KeyValueFilterBehavior
{
    private readonly string[]? _terms;

    private KeyValueFilterBehavior(KeyValueFilterMode mode, string[]? terms)
    {
        Mode = mode;
        _terms = terms;
    }

    public KeyValueFilterMode Mode { get; }

    public string[] Terms => _terms ?? [];

    public static KeyValueFilterBehavior Off => new(KeyValueFilterMode.Off, null);

    /// <summary>The default behavior: collect everything, replacing values of sensitive keys.</summary>
    public static KeyValueFilterBehavior DenyList => new(KeyValueFilterMode.DenyList, null);

    public static KeyValueFilterBehavior DenyListWith(params string[] extraDenyTerms) =>
        new(KeyValueFilterMode.DenyList, extraDenyTerms);

    public static KeyValueFilterBehavior AllowList(params string[] allowedTerms) =>
        new(KeyValueFilterMode.AllowList, allowedTerms);
}
