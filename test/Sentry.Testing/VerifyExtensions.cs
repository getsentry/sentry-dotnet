#if !__MOBILE__

using System.Text.Json;
using System.Text.RegularExpressions;

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
            .IgnoreMembers<SentryStackFrame>(
                _ => _.FileName,
                _ => _.LineNumber,
                _ => _.ColumnNumber,
                _ => _.InstructionOffset,
                _ => _.Package)
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
                .ToDictionary();
            writer.Serialize(items);
        }
    }

    private class DebugImageConverter : WriteOnlyJsonConverter<DebugImage>
    {
        public override void Write(VerifyJsonWriter writer, DebugImage obj)
        {
            obj.DebugId = ScrubAlphaNum(obj.DebugId);
            obj.DebugChecksum = ScrubAlphaNum(obj.DebugChecksum);
            obj.DebugFile = ScrubPath(obj.DebugFile);
            obj.CodeFile = ScrubPath(obj.CodeFile);
            obj.CodeId = ScrubAlphaNum(obj.CodeId);
            writer.WriteJson(obj);
        }
    }

    private class StackFrameConverter : WriteOnlyJsonConverter<SentryStackFrame>
    {
        public override void Write(VerifyJsonWriter writer, SentryStackFrame obj)
        {
            obj.FileName = ScrubPath(obj.FileName);
            obj.FunctionId = ScrubAlphaNum(obj.FunctionId);
            obj.InstructionAddress = ScrubAlphaNum(obj.InstructionAddress);
            obj.Package = obj.Package.Replace(new Regex("=[^,]+"), "=SCRUBBED");
            writer.WriteJson(obj);
        }
    }

    // Extension so we can use `nullableString?.Replace()`.
    private static string Replace(this string str, Regex regex, string replacement) => regex.Replace(str, replacement);

    private static string ScrubAlphaNum(string str) => str?.Replace(new Regex("[a-zA-Z0-9]"), "_");

    private static string ScrubPath(string str) => str?.Replace(new Regex(@"^.*[/\\]"), ".../");

    private static void WriteJson(this VerifyJsonWriter verifyWriter, IJsonSerializable @object)
    {
        using var stream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(stream, new() { Indented = true });
        @object.WriteTo(jsonWriter, null);
        jsonWriter.Flush();
        var str = Encoding.UTF8.GetString(stream.ToArray());

        // Note: this is not perfect because we don't respect indentation. of the surrounding objects.
        // Unfortunately, there doesn't seem to be the way to get current serialization depth.
        // There's `Indentation` and `IndentChar` but those are only relevant in combination with the current depth
        // should be available as `Top`, which is protected internal...
        // verifyWriter.WriteValue(str);

        // Therefore, we have the following best-effort approach of splitting lines and writing individually
        // which makes the JsonWriter add proper indentation.
        var lines = str.Replace("\r", "").Split('\n');
        var depth = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Note: we can't use WriteStartObject/EndObject because folloowing WriteRawValueWithScrubbers() fails with:
            //   Argon.JsonWriterException : Token Undefined in state ObjectStart would result in an invalid JSON object. Path '[0].Items[0].Payload.Source.objs[0]'.
            switch (line)
            {
                case "{":
                case "[":
                    verifyWriter.WriteRawValue(line);
                    depth++;
                    break;
                case "}":
                case "]":
                    verifyWriter.WriteRawValue(line);
                    depth--;
                    break;
                default:
                    var indent = new string(verifyWriter.IndentChar, verifyWriter.Indentation * depth);
                    // FYI: this always adds comma on the previous line before adding the given value. Can't help it.
                    verifyWriter.WriteRawValueWithScrubbers(indent + line.TrimEnd(','));
                    break;
            }
        }
    }
}
#endif
