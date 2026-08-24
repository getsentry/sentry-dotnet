using System.Diagnostics;
using System.Text.RegularExpressions;
using OpenTelemetry;

namespace Sentry.Samples.OpenTelemetry.MongoDB;

/// <summary>
/// An example OpenTelemetry span processor that redacts sensitive values from the MongoDB
/// <c>db.query.text</c> attribute before spans are exported to Sentry.
/// </summary>
/// <remarks>
/// When query-text capture is enabled (see <c>TracingOptions.QueryTextMaxLength</c> above), the
/// MongoDB driver records the full command - including field values - on the span. If those values
/// can contain PII, you should redact them before they leave your process.
/// <para>
/// A span processor is the right place to do this: the Sentry OTLP exporter sends spans straight to
/// Sentry and does NOT run them through the SDK's <c>BeforeSend</c>/<c>BeforeSendTransaction</c>
/// hooks, so the processor has to be registered before <c>AddSentryOtlpExporter</c> in the pipeline.
/// </para>
/// <para>
/// This example redacts the "contributor" field (a person's name). Adapt the field names and logic
/// to match the sensitive data in your own queries.
/// </para>
/// </remarks>
internal sealed partial class RedactSensitiveMongoData : BaseProcessor<Activity>
{
    // Matches "contributor": "<value>" in MongoDB's extended JSON (spacing may vary).
    [GeneratedRegex("(\"contributor\"\\s*:\\s*)\"[^\"]*\"")]
    private static partial Regex SensitiveField();

    public override void OnEnd(Activity activity)
    {
        if (activity.GetTagItem("db.query.text") is string queryText)
        {
            activity.SetTag("db.query.text", SensitiveField().Replace(queryText, "$1\"[Filtered]\""));
        }
    }
}
