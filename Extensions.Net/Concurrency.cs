using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Extensions
{
    public static class Concurrency
    {
        public static async Task<T> WhenAny<T>(this IEnumerable<Task<T>> tasks, CancellationTokenSource cancellationToken, Func<T, bool> predicate)
        {
            var taskList = tasks.ToList();

            Task<T> completedTask = null;

            taskList.ForEach(t => t.Start());

            while (taskList.Count > 0)
            {
                completedTask = await Task.WhenAny(taskList);
                taskList.Remove(completedTask);

                if (predicate(await completedTask))
                {
                    cancellationToken.Cancel(false);
                    break;
                }

                completedTask = null;
            }

            return completedTask == null ? default(T) : completedTask.Result;
        }

        public static void SkipOnError(this Action action, params Type[] exceptions)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (exceptions == null) return;
                foreach (var exc in exceptions)
                {
                    if (ex.GetType() == exc) return;
                }
                throw;
            }
        }
    }
}