public static class VerifyExtensions
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
            .IgnoreMembers<Request>(e => e.Env, e => e.Url, e => e.Headers)
            .IgnoreMembers<Transaction>(t => t.Release)
            .IgnoreMembers<SentryException>(e => e.Module, e => e.ThreadId);
    }

    class SpansConverter : WriteOnlyJsonConverter<IReadOnlyCollection<Span>>
    {
        public override void Write(VerifyJsonWriter writer, IReadOnlyCollection<Span> spans)
        {
            var ordered = spans
                .OrderBy(x => x.StartTimestamp)
                .ToList();

            writer.WriteStartArray();

            foreach (var span in ordered)
            {
                writer.Serialize(span);
            }

            writer.WriteEndArray();
        }
    }
}
