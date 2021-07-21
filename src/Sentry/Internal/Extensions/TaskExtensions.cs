using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Internal.Extensions
{
    internal static class TaskExtensions
    {
        public static async Task WithUncooperativeCancellationAsync(
            this Task task,
            CancellationToken cancellationToken)
        {
            var cancellationTask = Task.Delay(-1, cancellationToken);

            // Note: Task.WhenAny() doesn't throw
            var finishedTask = await Task.WhenAny(task, cancellationTask).ConfigureAwait(false);

            // Finalize and propagate exceptions
            await finishedTask.ConfigureAwait(false);
        }
    }
}
