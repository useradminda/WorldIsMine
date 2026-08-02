using System;
using System.Collections.Concurrent;

namespace WorldIsMine.Net.Runtime
{
    public sealed class MainThreadDispatcher
    {
        private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        public void Post(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            _queue.Enqueue(action);
        }

        public int Drain(int maximumActions = 256)
        {
            int count = 0;
            while (count < maximumActions && _queue.TryDequeue(out Action action))
            {
                action();
                count++;
            }

            return count;
        }
    }
}
