using System;
using System.Threading;
using System.Threading.Tasks;

namespace LSG.Infrastructure
{
    public class AsyncLock
    {
        private readonly SemaphoreSlim _asyncLocker = new SemaphoreSlim(1);

        public async Task LockAsync(Func<Task> func)
        {
            if (await _asyncLocker.WaitAsync(0))
            {
                try
                {
                    await func();
                }
                finally
                {
                    _asyncLocker.Release();
                }
            }
        }
    }
}