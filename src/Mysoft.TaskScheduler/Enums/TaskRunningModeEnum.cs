using System;
using System.Collections.Generic;
using System.Text;

namespace Mysoft.TaskScheduler.Enums
{
    /// <summary>
    /// 任务运行模式
    /// </summary>
    internal enum TaskRunningModeEnum
    {
        /// <summary>
        /// 串行
        /// </summary>
        Serial,

        /// <summary>
        /// 并行
        /// </summary>
        Parallel,
    }
}
