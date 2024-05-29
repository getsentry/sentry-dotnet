namespace Sentry.Protocol;

internal static partial class OriginValidator
{
    private const string ValidPartNamePattern = @"^[\w_-]*$";

#if NET8_0_OR_GREATER
    [GeneratedRegex(ValidPartNamePattern, RegexOptions.Compiled)]
    private static partial Regex ValidPartNameRegEx();
    private static readonly Regex ValidPartName = ValidPartNameRegEx();
#else
    private static readonly Regex ValidPartName = new (ValidPartNamePattern, RegexOptions.Compiled);
#endif

    public static bool IsValidPartName(string? value) => !string.IsNullOrEmpty(value) && !ValidPartName.IsMatch(value);
}
