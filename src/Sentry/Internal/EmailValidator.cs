using System.Text.RegularExpressions;

namespace Sentry.Internal;

/// <summary>
/// Helper class for email validation.
/// </summary>
internal static partial class EmailValidator
{
    private const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

#if NET9_0_OR_GREATER
    [GeneratedRegex(EmailPattern)]
    private static partial Regex Email { get; }
#elif NET8_0
    [GeneratedRegex(EmailPattern)]
    private static partial Regex EmailRegex();
    private static readonly Regex Email = EmailRegex();
#else
    private static readonly Regex Email = new(EmailPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Validates an email address.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <returns>True if the email is valid, false otherwise.</returns>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return true;
        }

        return Email.IsMatch(email);
    }
}
