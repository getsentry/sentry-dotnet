using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner
{
    internal class AsyncLock

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
Before:
		readonly SemaphoreSlim semaphore;
		readonly Task<Releaser> releaser;
After:
        private readonly SemaphoreSlim semaphore;
        private readonly Task<Releaser> releaser;
*/

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
Before:
		readonly SemaphoreSlim semaphore;
		readonly Task<Releaser> releaser;
After:
        private readonly SemaphoreSlim semaphore;
        private readonly Task<Releaser> releaser;
*/
    {
        private readonly SemaphoreSlim semaphore;
        private readonly Task<Releaser> releaser;

        public AsyncLock()
        {
            semaphore = new SemaphoreSlim(1);
            releaser = Task.FromResult(new Releaser(this));
        }

        public struct Releaser : IDisposable

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
Before:
			readonly AsyncLock toRelease;
After:
            private readonly AsyncLock toRelease;
*/

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
Before:
			readonly AsyncLock toRelease;
After:
            private readonly AsyncLock toRelease;
*/
        {
            private readonly AsyncLock toRelease;

            internal Releaser(AsyncLock toRelease)
            {
                this.toRelease = toRelease;
            }

            public void Dispose()
            {
                if (toRelease != null)
                    toRelease.semaphore.Release();
            }
        }

#if DEBUG
        public Task<Releaser> LockAsync([CallerMemberName] string callingMethod = null, [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
        {
            Debug.WriteLine("AsyncLock.LockAsync called by: " + callingMethod + " in file: " + path + " : " + line);
#else
		public Task<Releaser> LockAsync()
		{
#endif
            var wait = semaphore.WaitAsync();

            return wait.IsCompleted ?
                       releaser :
                       wait.ContinueWith((_, state) => new Releaser((AsyncLock)state),
                                         this, CancellationToken.None,
                                         TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}
