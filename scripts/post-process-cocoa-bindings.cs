#:package Microsoft.CodeAnalysis.CSharp@4.14.0
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: dotnet run post-process-swift-bindings.cs <path/to/SwiftApiDefinitions.cs>");
    return;
}

var code = File.ReadAllText(args[0]);
var tree = CSharpSyntaxTree.ParseText(code);
var root = tree.GetCompilationUnitRoot();
var filtered = root.RemoveNodes(
    root.DescendantNodes()
        .OfType<InterfaceDeclarationSyntax>()
        .Where(c => !Whitelist.Interfaces.Contains(c.Identifier.Text)),
    SyntaxRemoveOptions.KeepNoTrivia
);
File.WriteAllText(args[0], filtered?.ToFullString());

internal static class Whitelist
{
    public static readonly HashSet<string> Interfaces = new()
    {
        "Constants",
        "PrivateSentrySDKOnly",
        "SentryAttachment",
        "SentryBaggage",
        "SentryBreadcrumb",
        "SentryClient",
        "SentryClientReport",
        "SentryCurrentDateProvider",
        "SentryDebugImageProvider",
        "SentryDebugMeta",
        "SentryDiscardedEvent",
        "SentryDsn",
        "SentryEnvelope",
        "SentryEnvelopeHeader",
        "SentryEnvelopeItem",
        "SentryEnvelopeItemHeader",
        "SentryEvent",
        "SentryException",
        "SentryFeedback",
        "SentryFeedbackAPI",
        "SentryFormElementOutlineStyle",
        "SentryFrame",
        "SentryGeo",
        "SentryHttpStatusCodeRange",
        "SentryHub",
        "SentryId",
        "SentryIntegrationProtocol",
        "SentryLog",
        "SentryLogger",
        "SentryMeasurementUnit",
        "SentryMeasurementUnitDuration",
        "SentryMeasurementUnitFraction",
        "SentryMeasurementUnitInformation",
        "SentryMechanism",
        "SentryMechanismMeta",
        "SentryMessage",
        "SentryNSError",
        "SentryOptions",
        "SentryProfileOptions",
        "SentryRedactOptions",
        "SentryReplayApi",
        "SentryReplayBreadcrumbConverter",
        "SentryReplayEvent",
        "SentryReplayOptions",
        "SentryReplayRecording",
        "SentryRequest",
        "SentryRRWebEvent",
        "SentrySamplingContext",
        "SentryScope",
        "SentryScreenFrames",
        "SentrySDK",
        "SentrySdkInfo",
        "SentrySDKSettings",
        "SentrySerializable",
        "SentrySession",
        "SentrySpan",
        "SentrySpanContext",
        "SentrySpanId",
        "SentryStacktrace",
        "SentryThread",
        "SentryTraceContext",
        "SentryTraceHeader",
        "SentryTransactionContext",
        "SentryUser",
        "SentryUserFeedback",
        "SentryUserFeedbackConfiguration",
        "SentryUserFeedbackFormConfiguration",
        "SentryUserFeedbackThemeConfiguration",
        "SentryUserFeedbackWidgetConfiguration",
        "SentryVideoInfo",
        "SentryViewScreenshotOptions",
        "SentryViewScreenshotProvider",
    };
}
