using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: Sentry.Bindings.Cocoa.PostProcessor <path/to/ApiDefinitions.cs>");
    return;
}

var code = File.ReadAllText(args[0]);
var tree = CSharpSyntaxTree.ParseText(code);
var nodes = tree.GetCompilationUnitRoot()
    .Blacklist<MethodDeclarationSyntax>(
        // NSObject
        "IsEqual",
        "CopyWithZone",
        // PrivateSentrySDKOnly
        "StoreEnvelope",
        "CaptureEnvelope",
        "EnvelopeWithData",
        // SentryOptions
        "CaptureUserFeedback"
    )
    .Blacklist<DelegateDeclarationSyntax>(
        "SentryUserFeedbackConfigurationBlock"
    )
    .Blacklist<PropertyDeclarationSyntax>(
        "ConfigureUserFeedback",
        "Description",
        "EnableMetricKitRawPayload",
        "AppStartMeasurement",
        "AppStartMeasurementTimeoutInterval",
        "OnAppStartMeasurementAvailable",
        "SentryExperimentalOptions",
        "SpanDescription"
    )
    .Whitelist<InterfaceDeclarationSyntax>(
        "Constants",
        "ISentryRRWebEvent",
        "PrivateSentrySDKOnly",
        "SentryAttachment",
        "SentryBaggage",
        "SentryBreadcrumb",
        "SentryClient",
        "SentryDebugImageProvider",
        "SentryDebugMeta",
        "SentryDsn",
        "SentryEvent",
        "SentryException",
        "SentryFeedback",
        "SentryFeedbackAPI",
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
        "SentryReplayOptions",
        "SentryRequest",
        "SentryRRWebEvent",
        "SentrySamplingContext",
        "SentryScope",
        "SentryScreenFrames",
        "SentrySDK",
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
        "SentryViewScreenshotOptions",
        "SentryViewScreenshotProvider"
    );
File.WriteAllText(args[0], nodes.ToFullString());

internal static class FilterExtensions
{
    private static string GetIdentifier(SyntaxNode node)
    {
        return node switch
        {
            TypeDeclarationSyntax type => type.Identifier.Text,
            DelegateDeclarationSyntax del => del.Identifier.Text,
            MethodDeclarationSyntax method => method.Identifier.Text,
            PropertyDeclarationSyntax property => property.Identifier.Text,
            _ => throw new NotSupportedException(node.GetType().Name)
        };
    }

    public static CompilationUnitSyntax Blacklist<T>(
        this CompilationUnitSyntax root,
        params string[] names) where T : SyntaxNode
    {
        var nameSet = new HashSet<string>(names);
        var nodesToRemove = root.DescendantNodes()
            .OfType<T>()
            .Where(node => nameSet.Contains(GetIdentifier(node)));
        return root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia)!;
    }

    public static CompilationUnitSyntax Whitelist<T>(
        this CompilationUnitSyntax root,
        params string[] names) where T : SyntaxNode
    {
        var nameSet = new HashSet<string>(names);
        var nodesToRemove = root.DescendantNodes()
            .OfType<T>()
            .Where(node => !nameSet.Contains(GetIdentifier(node)));
        return root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia)!;
    }
}
