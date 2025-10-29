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
    .Blacklist<AttributeSyntax>(
        // error CS0246: The type or namespace name 'iOS' could not be found
        "iOS",
        // error CS0246: The type or namespace name 'Mac' could not be found
        "Mac",
        // error CS0117: 'PlatformName' does not contain a definition for 'iOSAppExtension'
        "Unavailable"
    )
    .Blacklist<AttributeListSyntax>("")
    .Blacklist<MethodDeclarationSyntax>(
        // error CS0114: 'SentryXxx.IsEqual(NSObject?)' hides inherited member 'NSObject.IsEqual(NSObject?)'.
        "Sentry*.IsEqual",
        // error CS0246: The type or namespace name '_NSZone' could not be found
        "Sentry*.CopyWithZone",
        // SentryEnvelope* is blacklisted
        "PrivateSentrySDKOnly.CaptureEnvelope",
        "PrivateSentrySDKOnly.EnvelopeWithData",
        "PrivateSentrySDKOnly.StoreEnvelope",
        // deprecated
        "Sentry*.CaptureUserFeedback"
    )
    .Blacklist<DelegateDeclarationSyntax>(
        // deprecated
        "SentryUserFeedbackConfigurationBlock"
    )
    .Blacklist<PropertyDeclarationSyntax>(
        // error CS0114: 'SentryXxx.Description' hides inherited member 'NSObject.Description'.
        "Sentry*.Description",
        // SentryAppStartMeasurement is not whitelisted
        "PrivateSentrySDKOnly.*AppStartMeasurement*",
        // deprecated
        "SentryOptions.ConfigureUserFeedback"
    )
    .Whitelist<InterfaceDeclarationSyntax>(
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
            AttributeSyntax attr => attr.Name.ToString(),
            AttributeListSyntax list => string.Join(",", list.Attributes.Select(a => a.Name.ToString())),
            _ => throw new NotSupportedException(node.GetType().Name)
        };
    }

    private static string GetQualifiedName(SyntaxNode node)
    {
        var identifier = GetIdentifier(node);
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is TypeDeclarationSyntax typeDecl)
            {
                return $"{typeDecl.Identifier.Text}.{identifier}";
            }
            parent = parent.Parent;
        }
        return identifier;
    }

    private static bool MatchesPattern(string name, string pattern)
    {
        if (pattern == name)
        {
            return true;
        }

        if (!pattern.Contains('*') && !pattern.Contains('?'))
        {
            return false;
        }

        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return Regex.IsMatch(name, regexPattern);
    }

    private static bool MatchesName(SyntaxNode node, string[] patterns)
    {
        var identifier = GetIdentifier(node);
        var qualifiedName = GetQualifiedName(node);
        foreach (var pattern in patterns)
        {
            var actualPattern = pattern.TrimStart('!');
            if (MatchesPattern(identifier, actualPattern) || MatchesPattern(qualifiedName, actualPattern))
            {
                return !pattern.StartsWith('!');
            }
        }
        return false;
    }

    public static CompilationUnitSyntax Blacklist<T>(
        this CompilationUnitSyntax root,
        params string[] names) where T : SyntaxNode
    {
        var nodesToRemove = root.DescendantNodes()
            .OfType<T>()
            .Where(node => MatchesName(node, names));
        return root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia)!;
    }

    public static CompilationUnitSyntax Whitelist<T>(
        this CompilationUnitSyntax root,
        params string[] names) where T : SyntaxNode
    {
        var nodesToRemove = root.DescendantNodes()
            .OfType<T>()
            .Where(node => !MatchesName(node, names));
        return root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia)!;
    }
}
