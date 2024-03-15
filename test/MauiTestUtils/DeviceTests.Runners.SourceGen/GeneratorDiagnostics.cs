using System.Diagnostics;
using Microsoft.CodeAnalysis;

<<<<<<< HEAD
namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.SourceGen
=======
namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.SourceGen;

internal static class GeneratorDiagnostics
>>>>>>> chore/net8-devicetests
{
	static class GeneratorDiagnostics
	{
		public static readonly DiagnosticDescriptor LoggingMessage = new DiagnosticDescriptor(
			id: "TST1001",
			title: "Logging Message",
			messageFormat: "{0}",
			category: "Logging",
			DiagnosticSeverity.Info,
			isEnabledByDefault: true);

<<<<<<< HEAD
		[Conditional("DEBUG")]
		public static void Log(this GeneratorExecutionContext context, string message) =>
			context.ReportDiagnostic(Diagnostic.Create(LoggingMessage, Location.None, message));
	}
}
=======
    [Conditional("DEBUG")]
    public static void Log(this GeneratorExecutionContext context, string message) =>
        context.ReportDiagnostic(Diagnostic.Create(LoggingMessage, Location.None, message));
}
>>>>>>> chore/net8-devicetests
