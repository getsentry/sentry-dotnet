using Quartz;
using Quartz.Impl.Triggers;

namespace Sentry.Quartz;

internal static class TriggerSchedule
{
    private static readonly string[] MonthNames = ["JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"];
    private static readonly string[] DayNames = ["SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT"];

    public static Action<SentryMonitorOptions>? ToMonitorConfig(ITrigger trigger)
    {
        if (trigger.CalendarName is not null)
        {
            return null;
        }

        switch (trigger)
        {
            case ICronTrigger cronTrigger:
                var crontab = ToCrontab(cronTrigger.CronExpressionString);
                var timeZone = ToIanaTimeZoneId(cronTrigger.TimeZone);
                if (crontab is null || timeZone is null)
                {
                    return null;
                }

                return options =>
                {
                    options.Interval(crontab);
                    options.TimeZone ??= timeZone;
                };
            case ISimpleTrigger simpleTrigger when ToInterval(simpleTrigger) is (var interval, var unit):
                return options => options.Interval(interval, unit);
            default:
                return null;
        }
    }

    // Quartz has a seconds field, an optional year field, '?' for an unused day field, and numbers days of the week
    // 1 (SUN) to 7 (SAT). Only expressions whose fields mean the same in Sentry's crontab are converted.
    internal static string? ToCrontab(string? cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return null;
        }

        var fields = cronExpression!.ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length is < 6 or > 7
            || !TryParseNumber(fields[0], 0, 59, out _)
            || (fields.Length == 7 && fields[6] != "*"))
        {
            return null;
        }

        var dayOfMonth = fields[3];
        var dayOfWeek = fields[5];
        if (QuartzApi.UnionsDayFields
                ? dayOfMonth == "?" && dayOfWeek == "?"
                : (dayOfMonth == "?") == (dayOfWeek == "?"))
        {
            return null;
        }

        // Quartz 4 runs on either day when both day fields restrict. Sentry does too, but only when neither field
        // starts with '*', so a '*' with a step is spelled out as a range there.
        var bothDaysRestrict = !IsAnyDay(dayOfMonth) && !IsAnyDay(dayOfWeek);
        string?[] crontab =
        [
            ToCrontabField(fields[1], 0, 59),
            ToCrontabField(fields[2], 0, 23),
            ToDayField(dayOfMonth, 1, 31, null, 0, bothDaysRestrict),
            ToCrontabField(fields[4], 1, 12, MonthNames),
            ToDayField(dayOfWeek, 1, 7, DayNames, -1, bothDaysRestrict)
        ];

        return crontab.Any(field => field is null) ? null : string.Join(" ", crontab);
    }

    private static bool IsAnyDay(string field) => field is "*" or "?";

    private static string? ToDayField(
        string field,
        int min,
        int max,
        string[]? names,
        int offset,
        bool spellOutStars)
    {
        if (IsAnyDay(field))
        {
            return "*";
        }

        var crontabField = ToCrontabField(field, min, max, names, offset, spellOutStars);
        if (crontabField is null)
        {
            return null;
        }

        // Quartz reads a day field that lists '*' as any day, like a lone '*'
        return field.Split(',').Contains("*") ? "*" : crontabField;
    }

    private static string? ToCrontabField(
        string field,
        int min,
        int max,
        string[]? names = null,
        int offset = 0,
        bool spellOutStar = false)
    {
        var terms = field.Split(',');
        for (var i = 0; i < terms.Length; i++)
        {
            if (ToCrontabTerm(terms[i], min, max, names, offset, spellOutStar) is not { } term)
            {
                return null;
            }

            terms[i] = term;
        }

        return string.Join(",", terms);
    }

    private static string? ToCrontabTerm(
        string term,
        int min,
        int max,
        string[]? names,
        int offset,
        bool spellOutStar)
    {
        string? step = null;
        var slash = term.IndexOf('/');
        if (slash >= 0)
        {
            if (!TryParseNumber(term.Substring(slash + 1), 1, int.MaxValue, out var stepValue))
            {
                return null;
            }

            step = "/" + stepValue.ToString(CultureInfo.InvariantCulture);
            term = term.Substring(0, slash);
        }

        if (term == "*")
        {
            return spellOutStar && step is not null
                ? Format(min + offset) + "-" + Format(max + offset) + step
                : "*" + step;
        }

        var dash = term.IndexOf('-');
        var startText = dash < 0 ? term : term.Substring(0, dash);
        var endText = dash < 0 ? null : term.Substring(dash + 1);

        // Quartz mishandles steps after names, so their meaning is uncertain
        var usesNames = !char.IsDigit(startText.FirstOrDefault()) || (endText is not null && !char.IsDigit(endText.FirstOrDefault()));
        if (step is not null && usesNames)
        {
            return null;
        }

        if (!TryParseValue(startText, min, max, names, out var start))
        {
            return null;
        }

        int end;
        if (endText is null)
        {
            if (step is null)
            {
                return Format(start + offset);
            }

            // Quartz's "a/n" runs from a to the end of the range, which Sentry needs written as a range
            end = max;
        }
        else if (!TryParseValue(endText, min, max, names, out end))
        {
            return null;
        }

        // Quartz wraps ranges whose end comes before their start, and mishandles ranges that start and end on the same value
        if (end <= start)
        {
            return null;
        }

        return Format(start + offset) + "-" + Format(end + offset) + step;
    }

    private static bool TryParseValue(
        string text,
        int min,
        int max,
        string[]? names,
        out int value)
    {
        var nameIndex = names is null ? -1 : Array.IndexOf(names, text);
        if (nameIndex >= 0)
        {
            value = nameIndex + min;
            return true;
        }

        return TryParseNumber(text, min, max, out value);
    }

    private static bool TryParseNumber(
        string text,
        int min,
        int max,
        out int value) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value >= min && value <= max;

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    internal static (int Interval, SentryMonitorInterval Unit)? ToInterval(ISimpleTrigger trigger)
    {
        var interval = trigger.RepeatInterval;
        if (trigger.RepeatCount != SimpleTriggerImpl.RepeatIndefinitely
            || interval <= TimeSpan.Zero
            || interval.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            return null;
        }

        var (count, unit) = interval.Ticks switch
        {
            var ticks when ticks % TimeSpan.TicksPerDay == 0 => (ticks / TimeSpan.TicksPerDay, SentryMonitorInterval.Day),
            var ticks when ticks % TimeSpan.TicksPerHour == 0 => (ticks / TimeSpan.TicksPerHour, SentryMonitorInterval.Hour),
            var ticks => (ticks / TimeSpan.TicksPerMinute, SentryMonitorInterval.Minute)
        };

        return count <= int.MaxValue ? ((int)count, unit) : null;
    }

    internal static string? ToIanaTimeZoneId(TimeZoneInfo? timeZone)
    {
        if (timeZone is null)
        {
            return null;
        }

        if (timeZone.Id == TimeZoneInfo.Utc.Id)
        {
            return "UTC";
        }

#if NET6_0_OR_GREATER
        if (timeZone.HasIanaId)
        {
            return timeZone.Id;
        }

        return TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZone.Id, out var ianaId) ? ianaId : null;
#else
        // .NET Framework can't convert Windows IDs, so only pass on IDs that look like an IANA ID
        return timeZone.Id.Contains("/") ? timeZone.Id : null;
#endif
    }
}
