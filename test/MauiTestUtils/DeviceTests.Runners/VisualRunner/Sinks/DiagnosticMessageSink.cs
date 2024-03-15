﻿#nullable enable
using System;
using Xunit;

namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner
{
    internal class DiagnosticMessageSink : DiagnosticEventSink
    {
        public DiagnosticMessageSink(Action<string> logger, string assemblyDisplayName, bool showDiagnostics)
        {
            if (showDiagnostics && logger != null)
            {
                DiagnosticMessageEvent += args => logger($"{assemblyDisplayName}: {args.Message.Message}");
            }
        }
    }
}
