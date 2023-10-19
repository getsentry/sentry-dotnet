#if !__MOBILE__
using Argon;
using Sentry.PlatformAbstractions;

namespace Sentry.Testing;

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
                _.Converters.Add(new DebugImageConverter());
                _.Converters.Add(new StackFrameConverter());
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
            .IgnoreMembers<SessionUpdate>(
                _ => _.Duration)
            .IgnoreMembers<Transaction>(
                _ => _.Release)
            .IgnoreMembers<SentryException>(
                _ => _.Module,
                _ => _.ThreadId)
            .IgnoreMembers<SentryThread>(_ => _.Id)
            .IgnoreStackTrace();
    }

    private class SpansConverter : WriteOnlyJsonConverter<IReadOnlyCollection<Span>>
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

    private class ContextsConverter : WriteOnlyJsonConverter<Contexts>
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
                .ToDict();
            writer.Serialize(items);
        }
    }

    private class DebugImageConverter : WriteOnlyJsonConverter<DebugImage>
    {
        private static readonly Regex PathRegex = new(@"^.*[/\\]", RegexOptions.Compiled);
        private static string ScrubPath(string str) => str?.Replace(PathRegex, ".../");

        public override void Write(VerifyJsonWriter writer, DebugImage obj)
        {
            obj.DebugId = ScrubAlphaNum(obj.DebugId);
            obj.DebugChecksum = ScrubAlphaNum(obj.DebugChecksum);
            obj.DebugFile = ScrubPath(obj.DebugFile);
            obj.CodeFile = ScrubPath(obj.CodeFile);
            obj.CodeId = ScrubAlphaNum(obj.CodeId);
            writer.Serialize(JToken.FromObject(obj));
        }
    }

    private class StackFrameConverter : WriteOnlyJsonConverter<SentryStackFrame>
    {
        private static readonly Regex PackageRegex = new("=[^,]+", RegexOptions.Compiled);

        public override void Write(VerifyJsonWriter writer, SentryStackFrame obj)
        {
            obj.FunctionId = obj.FunctionId is null ? null : 1;
            obj.InstructionAddress = obj.InstructionAddress is null ? null : 2;
            obj.Package = obj.Package.Replace(PackageRegex, "=SCRUBBED");

            if (RuntimeInfo.GetRuntime().IsMono())
            {
                // On Mono, these items only come through from the stack trace when the `--debug` flag is passed,
                // either to mono.exe or set in the MONO_ENV_OPTIONS environment variable.
                // Rider sets the `--debug` flag, but `dotnet test` does not.
                // Thus we can't reliably include them and have tests pass in both.
                obj.FileName = string.Empty;
                obj.LineNumber = null;
                obj.ColumnNumber = null;
                obj.AbsolutePath = null;
            }

            writer.Serialize(JToken.FromObject(obj));
        }
    }

    private static readonly Regex AlphaNumRegex = new("[a-zA-Z0-9]", RegexOptions.Compiled);

    private static string Replace(this string str, Regex regex, string replacement) => regex.Replace(str, replacement);

    private static string ScrubAlphaNum(string str) => str?.Replace(AlphaNumRegex, "_");
}
#endif
