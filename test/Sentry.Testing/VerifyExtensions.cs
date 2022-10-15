public static class VerifyExtensions
{
    public static SettingsTask IgnoreStandardSentryMembers(this SettingsTask settings)
    {
        return settings
            .ScrubMachineName()
            .ScrubUserName()
            .AddExtraSettings(_ =>
            {
                _.Converters.Add(new SpansConverter());
                _.Converters.Add(new ContextsConverter());
            })
            .IgnoreMembers("version", "elapsed")
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

    class ContextsConverter : WriteOnlyJsonConverter<Contexts>
    {
        public override void Write(VerifyJsonWriter writer, Contexts contexts)
        {
            var items = contexts
                .Where(_ => _.Key != "os" &&
                            _.Key != "Current Culture" &&
                            _.Key != "ThreadPool Info" &&
                            _.Key != "runtime" &&
                            _.Key != "Current UI Culture" &&
                            _.Key != "device" &&
                            _.Key != ".NET Framework" &&
                            _.Key != "app" &&
                            _.Key != "Memory Info" &&
                            _.Key != "Dynamic Code")
                .OrderBy(x => x.Key)
                .ToDictionary();
            writer.Serialize(items);
        }
    }
}
