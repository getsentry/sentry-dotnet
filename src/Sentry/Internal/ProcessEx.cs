using System;
using System.Diagnostics;

namespace Sentry.Internal
{
    internal static class ProcessEx
    {
        public static int GetCurrentProcessId()
        {
            using var process = Process.GetCurrentProcess();
            return process.Id;
        }

        public static bool IsProcessAlive(int processId)
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
