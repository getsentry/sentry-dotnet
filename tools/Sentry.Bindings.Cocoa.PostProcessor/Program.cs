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
    .InsertNamespace("Sentry.CocoaSdk")
    // rename conflicting SentryRRWebEvent (protocol vs. interface)
    .Rename<InterfaceDeclarationSyntax>("SentryRRWebEvent", "ISentryRRWebEvent", iface => iface.HasAttribute("Protocol"))
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
        // SentryEnvelope* is not whitelisted
        "PrivateSentrySDKOnly.CaptureEnvelope",
        "PrivateSentrySDKOnly.EnvelopeWithData",
        "PrivateSentrySDKOnly.StoreEnvelope",
        // deprecated
        "Sentry*.CaptureUserFeedback"
    )
    .Blacklist<DelegateDeclarationSyntax>(
        // SentryAppStartMeasurement is not whitelisted
        "SentryOnAppStartMeasurementAvailable",
        // deprecated
        "SentryUserFeedbackConfigurationBlock"
    )
    .Blacklist<PropertyDeclarationSyntax>(
        // error CS0114: 'SentryXxx.Description' hides inherited member 'NSObject.Description'.
        "Sentry*.Description",
        // SentryAppStartMeasurement is not whitelisted
        "PrivateSentrySDKOnly.*AppStartMeasurement*",
        // SentryStructuredLogAttribute is not whitelisted
        "SentryLog.Attributes",
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
    public static CompilationUnitSyntax Blacklist<T>(
        this CompilationUnitSyntax root,
        params string[] names) where T : SyntaxNode
    {
        var nodesToRemove = root.DescendantNodes()
            .OfType<T>()
            .Where(node => names.Any(node.Matches));
        return root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia)!;
    }

    public static CompilationUnitSyntax Whitelist<T>(
        this CompilationUnitSyntax root,
        params string[] names) where T : SyntaxNode
    {
        var nodesToRemove = root.DescendantNodes()
            .OfType<T>()
            .Where(node => !names.Any(node.Matches));
        return root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia)!;
    }

    public static CompilationUnitSyntax InsertNamespace(
        this CompilationUnitSyntax root,
        string name)
    {
        var namespaceDeclaration = SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName(name))
            .WithNamespaceKeyword(SyntaxFactory.Token(SyntaxKind.NamespaceKeyword)
                .WithLeadingTrivia(SyntaxFactory.EndOfLine("\n"))
                .WithTrailingTrivia(SyntaxFactory.Space))
            .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));
        return SyntaxFactory.CompilationUnit()
            .WithUsings(root.Usings)
            .AddMembers(namespaceDeclaration.WithMembers(root.Members));
    }

    public static CompilationUnitSyntax Rename<T>(
        this CompilationUnitSyntax root,
        string oldName,
        string newName,
        Func<T, bool>? predicate = null) where T : SyntaxNode
    {
        var replacements = new Dictionary<SyntaxNode, SyntaxNode>();
        foreach (var node in root.DescendantNodes().OfType<T>())
        {
            if (node.GetIdentifier() == oldName && (predicate == null || predicate(node)))
            {
                replacements[node] = node.WithIdentifier(newName);
            }
        }
        return root.ReplaceNodes(replacements.Keys, (orig, _) => replacements[orig]);
    }
}

internal static class SyntaxNodeExtensions
{
    public static string GetIdentifier(this SyntaxNode node)
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

    public static SyntaxNode WithIdentifier(this SyntaxNode node, string newName)
    {
        var identifier = SyntaxFactory.Identifier(newName);
        return node switch
        {
            InterfaceDeclarationSyntax iface => iface.WithIdentifier(identifier),
            ClassDeclarationSyntax cls => cls.WithIdentifier(identifier),
            StructDeclarationSyntax str => str.WithIdentifier(identifier),
            EnumDeclarationSyntax enm => enm.WithIdentifier(identifier),
            DelegateDeclarationSyntax del => del.WithIdentifier(identifier),
            MethodDeclarationSyntax method => method.WithIdentifier(identifier),
            PropertyDeclarationSyntax property => property.WithIdentifier(identifier),
            _ => throw new NotSupportedException(node.GetType().Name)
        };
    }

    public static string GetQualifiedName(this SyntaxNode node)
    {
        var identifier = node.GetIdentifier();
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

    public static bool Matches(this SyntaxNode node, string pattern)
    {
        var actualPattern = pattern.TrimStart('!');
        if (node.GetIdentifier().Matches(actualPattern) || node.GetQualifiedName().Matches(actualPattern))
        {
            return !pattern.StartsWith('!');
        }
        return false;
    }

    public static bool HasAttribute(this SyntaxNode node, string attributeName)
    {
        return node switch
        {
            InterfaceDeclarationSyntax iface => iface.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => attr.Name.ToString() == attributeName),
            ClassDeclarationSyntax cls => cls.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => attr.Name.ToString() == attributeName),
            StructDeclarationSyntax str => str.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => attr.Name.ToString() == attributeName),
            EnumDeclarationSyntax enm => enm.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => attr.Name.ToString() == attributeName),
            MethodDeclarationSyntax method => method.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => attr.Name.ToString() == attributeName),
            PropertyDeclarationSyntax property => property.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => attr.Name.ToString() == attributeName),
            _ => false
        };
    }
}

internal static class StringExtensions
{
    public static bool Matches(this string str, string pattern)
    {
        if (pattern == str)
        {
            return true;
        }

        if (!pattern.Contains('*') && !pattern.Contains('?'))
        {
            return false;
        }

        var regex = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return Regex.IsMatch(str, regex);
    }
}
