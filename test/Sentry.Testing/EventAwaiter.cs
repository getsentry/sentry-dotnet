using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Testing
{
    public class EventAwaiter : IDisposable
    {
        private readonly TaskCompletionSource<object> _tcs =
            new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly object _owner;
        private readonly EventInfo _eventInfo;
        private readonly Delegate _handlerDelegate;

        public EventAwaiter(object owner, EventInfo eventInfo)
        {
            _owner = owner;
            _eventInfo = eventInfo;

            _handlerDelegate = Delegate.CreateDelegate(
                eventInfo.EventHandlerType,
                typeof(EventAwaiter).GetMethod(nameof(EventHandler))!
            );

            _eventInfo.AddEventHandler(_owner, _handlerDelegate);
        }

        private void EventHandler(object sender, object arg) =>
            _tcs.TrySetResult(arg);

        public async Task<object> WaitUntilTriggeredAsync(CancellationToken cancellationToken = default)
        {
            using var _ = cancellationToken.Register(() => _tcs.TrySetCanceled(cancellationToken));
            return await _tcs.Task.ConfigureAwait(false);
        }

        public void Dispose() =>
            _eventInfo.RemoveEventHandler(_owner, _handlerDelegate);
    }

    public static class Event
    {
        public static async Task<object> WaitUntilTriggeredAsync(
            object owner,
            string eventName,
            CancellationToken cancellationToken = default)
        {
            var eventInfo =
                owner.GetType().GetEvent(eventName) ??
                throw new InvalidOperationException("Event handler is invalid.");

            using var awaiter = new EventAwaiter(owner, eventInfo);
            return await awaiter.WaitUntilTriggeredAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async Task<T> WaitUntilTriggeredAsync<T>(
            object owner,
            string eventName,
            CancellationToken cancellationToken = default) =>
            (T)await WaitUntilTriggeredAsync(owner, eventName, cancellationToken).ConfigureAwait(false);
    }
}
