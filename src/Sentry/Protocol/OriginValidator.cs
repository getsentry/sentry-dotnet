namespace Sentry.Protocol;

internal static partial class OriginValidator
{
    private const string ValidOriginPattern = @"^(auto|manual)(\.[\w_-]+){0,3}$";

#if NET8_0_OR_GREATER
    [GeneratedRegex(ValidOriginPattern, RegexOptions.Compiled)]
    private static partial Regex ValidOriginRegEx();
    private static readonly Regex ValidOrigin = ValidOriginRegEx();
#else
    private static readonly Regex ValidOrigin = new (ValidOriginPattern, RegexOptions.Compiled);
#endif

    public static bool IsValidOrigin(string? value) => !string.IsNullOrEmpty(value) && ValidOrigin.IsMatch(value);
}
