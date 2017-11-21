using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NewLife.Log;

namespace System.Threading.Tasks
{
    /// <summary>任务助手</summary>
    public static class TaskHelper
    {
        /// <summary>捕获异常并输出日志</summary>
        /// <param name="task"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static Task LogException(this Task task, ILog log = null)
        {
            if (task == null) return null;

            if (log == null) log = XTrace.Log;
            if (log == Logger.Null || !log.Enable) return task;

            return task.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null && t.Exception.InnerException != null) log.Error(null, t.Exception.InnerException);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }     
    }
}