static class VerifyExtensions
{
    public static SettingsTask IgnoreStandardSentryMembers(this SettingsTask settings)
    {
        return settings.ModifySerialization(
            p =>
            {
                p.AddExtraSettings(x => x.Converters.Add(new SpansConverter()));
                p.IgnoreMembersWithType<Contexts>();
                p.IgnoreMembersWithType<SdkVersion>();
                p.IgnoreMembersWithType<DateTimeOffset>();
                p.IgnoreMembersWithType<SpanId>();
                p.IgnoreMembersWithType<SentryId>();
                p.IgnoreMembers<SentryEvent>(e => e.Modules, e => e.Release);
                p.IgnoreMembers<Transaction>(t => t.Release);
                p.IgnoreMembers<SentryException>(e => e.Module, e => e.ThreadId);
            });
    }
}
