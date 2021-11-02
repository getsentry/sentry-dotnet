using System;
using System.Diagnostics;

namespace Sentry.Internal
{
    internal sealed class ActiveProcessInfo : IActiveProcessInfo
    {
        public static IActiveProcessInfo Instance { get; } = new ActiveProcessInfo();

        public int GetCurrentProcessId()
        {
#if NET5_0_OR_GREATER
            return Environment.ProcessId;
#else
            using var process = Process.GetCurrentProcess();
            return process.Id;
#endif
        }

        public bool IsProcessActive(int processId)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch (ArgumentException)
            {
                // Thrown when the given ID does not correspond to an existing process
                return false;
            }
        }
    }
}
