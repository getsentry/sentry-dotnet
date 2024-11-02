using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Sentry.Analyzers.FileSystemAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Sentry.Analyzers.Tests;

public class FileSystemAnalyzerTests
{
    [Fact]
    public async Task Verify_UsingMethodsOnFileClass_TriggersAUseFileSystemWrapperWarning()
    {
        const string text = """
                            using System.IO;

                            namespace Sentry.Internal;

                            public class SomeClass
                            {
                                public void SomeMethod()
                                {
                                    var t = {|SN0001:File.Exists|}("fileName.txt");
                                }
                            }

                            """;

        await Verifier.VerifyAnalyzerAsync(text);
    }

    [Fact]
    public async Task Verify_UsingMethodsOnDirectoryClass_TriggersAUseFileSystemWrapperWarning()
    {
        const string text = """
                            using System.IO;

                            namespace Sentry.Internal;

                            public class SomeClass
                            {
                                public void SomeMethod()
                                {
                                    var t = {|SN0001:Directory.Exists|}("fileName.txt");
                                }
                            }

                            """;

        await Verifier.VerifyAnalyzerAsync(text);
    }

    [Fact]
    public async Task Verify_UsingFileInfoClass_TriggersAUseFileSystemWrapperWarning()
    {
        const string text = """
                            using System.IO;

                            namespace Sentry.Internal;

                            public class SomeClass
                            {
                                public void SomeMethod()
                                {
                                    var f = {|SN0001:new FileInfo("fileName.txt")|};
                                    var t = {|SN0001:f.Exists|};
                                }
                            }

                            """;

        await Verifier.VerifyAnalyzerAsync(text);
    }

    [Fact]
    public async Task Verify_UsingDirectoryInfoClass_TriggersAUseFileSystemWrapperWarning()
    {
        const string text = """
                            using System.IO;

                            namespace Sentry.Internal;

                            public class SomeClass
                            {
                                public void SomeMethod()
                                {
                                    var d = {|SN0001:new DirectoryInfo("directoryName")|};
                                    var t = {|SN0001:d.Exists|};
                                }
                            }

                            """;

        await Verifier.VerifyAnalyzerAsync(text);
    }

    [Fact]
    public async Task Verify_AccessingFileSystemWithinIFileSystemImplementation_ProducesNoWarning()
    {
        const string text = """
                            using System.IO;

                            namespace Sentry.Internal;

                            public class SomeClass : IFileSystem
                            {
                                public void SomeMethod()
                                {
                                    var d = new DirectoryInfo("directoryName");
                                }
                            }

                            public interface IFileSystem {}
                            """;

        await Verifier.VerifyAnalyzerAsync(text);
    }

    [Fact]
    public async Task Verify_ContainingTypeNull_DoesNotThrow()
    {
        // Containing type will be null when namespace member access will be checked.
        var testCode = """
                       internal class TestClass
                       {
                           public void TestMethod()
                           {
                               _ = System.String.Empty;
                           }
                       }
                       """;

        await Verifier.VerifyAnalyzerAsync(testCode);
    }
}
