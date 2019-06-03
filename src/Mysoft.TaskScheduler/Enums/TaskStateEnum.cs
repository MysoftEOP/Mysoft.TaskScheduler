using System;
using System.Collections.Generic;
using System.Text;

namespace Mysoft.TaskScheduler.Enums
{
    /// <summary>
    /// 任务状态枚举
    /// </summary>
    public enum TaskStateEnum
    {
        /// <summary>
        /// 已入队
        /// </summary>
        Enqueued,

        /// <summary>
        /// 执行中
        /// </summary>
        Processing,

        /// <summary>
        /// 等待中
        /// 该状态发生在有前置任务需要先执行的情况下
        /// </summary>
        Awaiting,

        /// <summary>
        /// 执行成功
        /// </summary>
        Succeeded,

        /// <summary>
        /// 执行失败
        /// </summary>
        Failed,

        /// <summary>
        /// 已删除
        /// </summary>
        Deleted,

        /// <summary>
        /// 计划执行
        /// </summary>
        Scheduled
    }
}
