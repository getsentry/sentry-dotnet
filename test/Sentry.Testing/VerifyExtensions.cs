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
            .IgnoreMembers<SentryEvent>(
                _ => _.Modules,
                _ => _.Release)
            .IgnoreMembers<Request>(
                _ => _.Env,
                _ => _.Url,
                _ => _.Headers)
            .IgnoreMembers<Transaction>(
                _ => _.Release)
            .IgnoreMembers<SentryException>(
                _ => _.Module,
                _ => _.ThreadId)
            .IgnoreMembers<SentryThread>(_ => _.Id)
            .IgnoreMembers<SentryStackFrame>(
                _ => _.FileName,
                _ => _.LineNumber,
                _ => _.ColumnNumber,
                _ => _.InstructionOffset,
                _ => _.Package)
            .IgnoreStackTrace();
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
