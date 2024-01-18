#nullable enable
using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner;

internal class DiagnosticMessageSink : Xunit.Sdk.LongLivedMarshalByRefObject, IMessageSink
{
    private Action<string> _logger;
    private string _assemblyDisplayName;
    private bool _showDiagnostics;

    public DiagnosticMessageSink(Action<string> logger, string assemblyDisplayName, bool showDiagnostics)
    {
        _logger = logger;
        _assemblyDisplayName = assemblyDisplayName;
        _showDiagnostics = showDiagnostics;
    }

    public bool OnMessage(IMessageSinkMessage message)
    {
        if (_showDiagnostics)
        {
            _logger($"{_assemblyDisplayName}: {message}");
        }

        return true;
    }
}
