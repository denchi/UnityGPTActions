using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GPTUnity.Settings;
using UnityEditor;

namespace Mcp
{
    public static class EditorMainThread
    {
        private const int MaxInFlight = 4;
        private static readonly ConcurrentQueue<Func<Task>> _queue = new ConcurrentQueue<Func<Task>>();
        private static int _inFlight;
        private static bool _updateHooked;

        public static Task<T> RunAsync<T>(Func<Task<T>> func)
        {
            if (UseUpdateQueue())
                return Enqueue(func);

            return RunWithDelayCall(func);
        }

        private static Task<T> Enqueue<T>(Func<Task<T>> func)
        {
            EnsureUpdateHooked();
            var tcs = new TaskCompletionSource<T>();
            _queue.Enqueue(async () =>
            {
                try
                {
                    var result = await func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        private static Task<T> RunWithDelayCall<T>(Func<Task<T>> func)
        {
            var tcs = new TaskCompletionSource<T>();
            EditorApplication.delayCall += async () =>
            {
                try
                {
                    var result = await func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            };
            return tcs.Task;
        }

        private static void EnsureUpdateHooked()
        {
            if (_updateHooked)
                return;
            _updateHooked = true;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            while (_inFlight < MaxInFlight && _queue.TryDequeue(out var work))
            {
                Interlocked.Increment(ref _inFlight);
                _ = RunWork(work);
            }
        }

        private static async Task RunWork(Func<Task> work)
        {
            try
            {
                await work();
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }

        private static bool UseUpdateQueue()
        {
            var settings = ChatSettings.instance;
            return settings != null && settings.McpUseUpdateQueue;
        }
    }
}
