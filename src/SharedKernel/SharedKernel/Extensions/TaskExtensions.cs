using System;
using System.Threading;
using System.Threading.Tasks;

namespace LSG.SharedKernel.Extensions
{
    public static class TaskExtensions
    {
        private static readonly TaskFactory TaskFactory = new
            TaskFactory(CancellationToken.None,
                TaskCreationOptions.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);

        public static async Task TimeoutAfterAsync(this Task task, TimeSpan waitTime)
        {
            var delay = Task.Delay(waitTime);
            var res = await Task.WhenAny(task, delay);
            if (res == delay)
            {
                throw new TimeoutException($"task not completed because timeup {waitTime.TotalMilliseconds} ms");
            }
        }


        public static void RunAsFireForget(this Action action)
        {
            Task.Run(action);
        }

        public static void RunOnBackgroundAsync(Func<Task> func, TaskCreationOptions options = TaskCreationOptions.None)
        {
            TaskFactory.StartNew(func, options);
        }
    }
}