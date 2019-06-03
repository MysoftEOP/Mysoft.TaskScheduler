using Hangfire;
using Hangfire.States;
using Hangfire.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mysoft.TaskScheduler.Hangfire
{
    /// <summary>
    /// Hangfire状态扩展类
    /// </summary>
    internal static class IStateExtensions
    {
        /// <summary>
        /// 获取任务参数
        /// </summary>
        /// <param name="context"></param>
        /// <param name="paraName"></param>
        /// <returns></returns>
        internal static string GetTaskParameter(this ApplyStateContext context, string paraName)
        {
            return context.Connection.GetJobParameter(context.BackgroundJob.Id, paraName);
        }
    }
}
