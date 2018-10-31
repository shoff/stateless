﻿using System.Threading.Tasks;

namespace Stateless
{
    internal static class TaskResult
    {
        internal static readonly Task done = FromResult(1);

        private static Task<T> FromResult<T>(T value)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);
            return tcs.Task;
        }
    }
}