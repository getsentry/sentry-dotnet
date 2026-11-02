using System.Text.RegularExpressions;

namespace Sentry.Internal;

/// <summary>
/// Validates the crontab format Sentry accepts for monitor schedules.
/// </summary>
/// <remarks>
/// Also compiled into Sentry.Hangfire, which can't see Sentry's internals because it isn't strong-named.
/// </remarks>
internal static partial class CrontabValidator
{
    // Breakdown of the validation regex pattern:
    // For each time field (minute, hour, day, month, weekday):
    // - Allows * for "any value"
    // - Allows */n for step values where n must be any positive integer (except zero)
    // - Allows single values within their valid ranges
    // - Allows ranges (e.g., 8-10)
    // - Allows step values with ranges (e.g., 8-18/4)
    // - Allows lists of values and ranges (e.g., 6,8,9 or 8-10,12-14)
    // - Allows weekday names (MON, TUE, WED, THU, FRI, SAT, SUN)
    //
    // Valid ranges for each field:
    // - Minutes: 0-59
    // - Hours: 0-23
    // - Days: 1-31
    // - Months: 1-12
    // - Weekdays: 0-7 (0 and 7 both represent Sunday) or MON-SUN
    private const string ValidCrontabPattern = @"^(\*(\/([1-9][0-9]*))?|([0-5]?\d)|([0-5]?\d)-([0-5]?\d)(\/([1-9][0-9]*))?)(,(\*(\/([1-9][0-9]*))?|([0-5]?\d)|([0-5]?\d)-([0-5]?\d)(\/([1-9][0-9]*))?))*(\s+)(\*(\/([1-9][0-9]*))?|([01]?\d|2[0-3])|([01]?\d|2[0-3])-([01]?\d|2[0-3])(\/([1-9][0-9]*))?)(,(\*(\/([1-9][0-9]*))?|([01]?\d|2[0-3])|([01]?\d|2[0-3])-([01]?\d|2[0-3])(\/([1-9][0-9]*))?))*(\s+)(\*(\/([1-9][0-9]*))?|([1-9]|[12]\d|3[01])|([1-9]|[12]\d|3[01])-([1-9]|[12]\d|3[01])(\/([1-9][0-9]*))?)(,(\*(\/([1-9][0-9]*))?|([1-9]|[12]\d|3[01])|([1-9]|[12]\d|3[01])-([1-9]|[12]\d|3[01])(\/([1-9][0-9]*))?))*(\s+)(\*(\/([1-9][0-9]*))?|([1-9]|1[0-2])|([1-9]|1[0-2])-([1-9]|1[0-2])(\/([1-9][0-9]*))?)(,(\*(\/([1-9][0-9]*))?|([1-9]|1[0-2])|([1-9]|1[0-2])-([1-9]|1[0-2])(\/([1-9][0-9]*))?))*(\s+)(\*(\/([1-9][0-9]*))?|[0-7]|(MON|TUE|WED|THU|FRI|SAT|SUN)|[0-7]-[0-7](\/([1-9][0-9]*))?|(MON|TUE|WED|THU|FRI|SAT|SUN)-(MON|TUE|WED|THU|FRI|SAT|SUN)(\/([1-9][0-9]*))?)(,(\*(\/([1-9][0-9]*))?|[0-7]|(MON|TUE|WED|THU|FRI|SAT|SUN)|[0-7]-[0-7](\/([1-9][0-9]*))?|(MON|TUE|WED|THU|FRI|SAT|SUN)-(MON|TUE|WED|THU|FRI|SAT|SUN)(\/([1-9][0-9]*))?))*$";

#if NET9_0_OR_GREATER
    [GeneratedRegex(ValidCrontabPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
    private static partial Regex ValidCrontab { get; }
#elif NET8_0
    [GeneratedRegex(ValidCrontabPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
    private static partial Regex ValidCrontabRegex();
    private static readonly Regex ValidCrontab = ValidCrontabRegex();
#else
    private static readonly Regex ValidCrontab = new(ValidCrontabPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
#endif

    public static bool IsValid(string crontab) => ValidCrontab.IsMatch(crontab);
}
