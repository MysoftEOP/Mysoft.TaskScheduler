using System;
using System.Collections.Generic;
using System.Text;

namespace Mysoft.TaskScheduler.Enums
{
    /// <summary>
    /// 任务执行类型枚举
    /// </summary>
    public enum TaskExecutionTypeEnum
    {
        /// <summary>
        /// 执行
        /// </summary>
        Do = 1,

        /// <summary>
        /// 回滚
        /// </summary>
        Undo = 2
    }
}
