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

root = root?.RemoveNodes(
    root.DescendantNodes()
        .OfType<EnumDeclarationSyntax>()
        .Where(c => !Whitelist.Enums.Contains(c.Identifier.Text)),
    SyntaxRemoveOptions.KeepNoTrivia
);

root = root?.RemoveNodes(
    root.DescendantNodes()
        .OfType<InterfaceDeclarationSyntax>()
        .Where(c => !Whitelist.Interfaces.Contains(c.Identifier.Text)),
    SyntaxRemoveOptions.KeepNoTrivia
);

File.WriteAllText(args[0], root?.ToFullString());

internal static class Whitelist
{
    public static readonly HashSet<string> Enums = new()
    {
        "SentryFeedbackSource",
        "SentryLevel",
        "SentryStructuredLogLevel",
        "SentryProfileLifecycle",
        "SentryReplayQuality",
        "SentryReplayType",
        "SentryRRWebEventType",
        "SentrySessionStatus",
        "SentryTransactionNameSource",
    };

    public static readonly HashSet<string> Interfaces = new()
    {
        "SentryAppState",
        "SentryClientReport",
        "SentryCurrentDateProvider",
        "SentryDiscardedEvent",
        "SentryEnvelope",
        "SentryEnvelopeHeader",
        "SentryEnvelopeItem",
        "SentryFeedback",
        "SentryFormElementOutlineStyle",
        "SentryId",
        "SentryLog",
        "SentryLogger",
        "SentryProfileOptions",
        "SentryRedactOptions",
        "SentryReplayBreadcrumbConverter",
        "SentryReplayEvent",
        "SentryReplayOptions",
        "SentryReplayRecording",
        "SentryRRWebEvent",
        "SentrySDK",
        "SentrySdkInfo",
        "SentrySDKSettings",
        "SentrySerializationSwift",
        "SentrySession",
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
