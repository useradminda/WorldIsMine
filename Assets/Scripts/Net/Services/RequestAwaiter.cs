using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorldIsMine.Net.Services
{
    internal sealed class RequestAwaiter<T>
    {
        private readonly object _gate = new object();
        private TaskCompletionSource<T> _pending;

        public TaskCompletionSource<T> Begin()
        {
            lock (_gate)
            {
                if (_pending != null)
                    throw new InvalidOperationException($"A {typeof(T).Name} request is already pending.");

                _pending = new TaskCompletionSource<T>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                return _pending;
            }
        }

        public void Complete(T value)
        {
            TaskCompletionSource<T> pending;
            lock (_gate)
                pending = _pending;
            pending?.TrySetResult(value);
        }

        public void Fail(Exception exception)
        {
            TaskCompletionSource<T> pending;
            lock (_gate)
                pending = _pending;
            pending?.TrySetException(exception);
        }

        public void End(TaskCompletionSource<T> owner)
        {
            lock (_gate)
            {
                if (ReferenceEquals(_pending, owner))
                    _pending = null;
            }
        }

        public static async Task<T> WaitAsync(
            Task<T> responseTask,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            Task delay = Task.Delay(timeout, cancellationToken);
            Task completed = await Task.WhenAny(responseTask, delay).ConfigureAwait(false);
            if (completed == responseTask)
                return await responseTask.ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException($"Waiting for {typeof(T).Name} timed out after {timeout}.");
        }
    }
}
