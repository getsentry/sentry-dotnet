static class VerifyExtensions
{
    public static SettingsTask IgnoreStandardSentryMembers(this SettingsTask settings)
    {
        return settings
            .AddExtraSettings(x => x.Converters.Add(new SpansConverter()))
            .IgnoreMembersWithType<Contexts>()
            .IgnoreMembersWithType<SdkVersion>()
            .IgnoreMembersWithType<DateTimeOffset>()
            .IgnoreMembersWithType<SpanId>()
            .IgnoreMembersWithType<SentryId>()
            .IgnoreMembers<SentryEvent>(e => e.Modules, e => e.Release)
            .IgnoreMembers<Transaction>(t => t.Release)
            .IgnoreMembers<SentryException>(e => e.Module, e => e.ThreadId);
    }
}
