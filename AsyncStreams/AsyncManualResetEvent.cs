using System.Threading;
using System.Threading.Tasks;

namespace AsyncStreams
{
    internal class AsyncManualResetEvent
    {
        private volatile TaskCompletionSource<bool> _eventTcs = new TaskCompletionSource<bool>();

        public Task WaitAsync()
        {
            return _eventTcs.Task;
        }

        public void Set()
        {
            _eventTcs.TrySetResult(true);
        }

        public void Reset()
        {
            while (true)
            {
                var currentTcs = _eventTcs;
                if (!currentTcs.Task.IsCompleted)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref _eventTcs, new TaskCompletionSource<bool>(), currentTcs) ==
                    currentTcs)
                {
                    return;
                }
                    
            }
        }
    }
}