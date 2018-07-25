using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Sentry.Protocol.Tests
{
    public class JsonDependencyTests
    {
        [Theory]
        [MemberData(nameof(GetSentryProtocolCsharpFiles))]
        public void Sentry_Protocol_DoesNotDependOnJsonNet(string path)
        {
            // To allow Sentry.Protocol to be pulled out to its own package
            // and make sure only the package 'Sentry' depends on Newtonsoft.Json
            // or any other serialization library
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(File.ReadAllText(path));
            {
                foreach (var @using in syntaxTree
                    .GetRoot()
                    .DescendantNodesAndSelf()
                    .OfType<CompilationUnitSyntax>()
                    .SelectMany(c => c.Usings))
                {

                    var @namespace = @using.Name.ToString();
                    Assert.DoesNotContain("Newtonsoft", @namespace);
                    Assert.DoesNotContain("Json", @namespace);
                }
            }
        }

        public static IEnumerable<object[]> GetSentryProtocolCsharpFiles()
        {
            var sentryProtocolTypes = typeof(Breadcrumb).Assembly
                .GetTypes()
                .Where(t => t.Namespace.StartsWith("Sentry.Protocol"))
                .ToDictionary(k => k.Name + ".cs");

            foreach (var source in Directory.EnumerateFiles("../../../../../src/Sentry.Protocol/", "*.cs", SearchOption.AllDirectories))
            {
                if (sentryProtocolTypes.ContainsKey(Path.GetFileName(source)))
                {
                    yield return new object[] { source };
                }
            }
        }
    }
}
