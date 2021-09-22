using System;
using Sentry.Internal;

namespace Sentry.Testing
{
    internal class FakeActiveProcessInfo : IActiveProcessInfo
    {
        private readonly int _processId;

        public FakeActiveProcessInfo(int processId = 1) => _processId = processId;

        public int GetCurrentProcessId() => _processId;

        public bool IsProcessActive(int processId) => _processId == processId;
    }
}
