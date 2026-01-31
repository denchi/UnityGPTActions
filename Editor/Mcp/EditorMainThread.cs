using System;
using System.Threading.Tasks;
using UnityEditor;

namespace Mcp
{
    public static class EditorMainThread
    {
        public static Task<T> RunAsync<T>(Func<Task<T>> func)
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
    }
}
