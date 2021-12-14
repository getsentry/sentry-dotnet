using System.Diagnostics;

namespace Ben.Demystifier.Test
{
    public class NonThrownException
    {
        [Fact]
        public async Task Current()
        {
            // Arrange
            EnhancedStackTrace est = null;

            // Act
            await Task.Run(() => est = EnhancedStackTrace.Current()).ConfigureAwait(false);

            // Assert
            var stackTrace = est.ToString();
            stackTrace = LineEndingsHelper.RemoveLineEndings(stackTrace);
            var trace = stackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                // Remove Full framework entries
                .Where(s => !s.StartsWith("   at bool System.Threading._ThreadPoolWaitCallbac") &&
                       !s.StartsWith("   at void System.Threading.Tasks.Task.System.Thre"));


            Assert.Equal(
                new[] {
                    "   at bool System.Threading.ThreadPoolWorkQueue.Dispatch()",
#if NET6_0_OR_GREATER
                    "   at void System.Threading.PortableThreadPool+WorkerThread.WorkerThreadStart()",
                    "   at void System.Threading.Thread.StartCallback()",
#endif
                },
                trace);
        }
    }
}
