using Mysoft.TaskScheduler.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mysoft.TaskScheduler.Hangfire
{
    /// <summary>
    /// Hangfire任务状态变更参数
    /// </summary>
    internal class TaskStateChangeEventArgs : EventArgs
    {
        /// <summary>
        /// 任务Id
        /// </summary>
        internal string Id { get; set; }

        /// <summary>
        /// 状态名称
        /// </summary>
        internal string StateName { get; set; }

        /// <summary>
        /// 任务执行类型
        /// Do or Undo
        /// </summary>
        internal TaskExecutionTypeEnum ExecutionType { get; set; }

        /// <summary>
        /// 回滚任务Id
        /// </summary>
        internal string UndoTaskId { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        internal string TaskName { get; set; }

        /// <summary>
        /// 执行任务Id链
        /// </summary>
        internal List<string> TaskDoIdChain { get; set; }

        /// <summary>
        /// 回滚任务Id链
        /// </summary>
        internal List<string> TaskUndoIdChain { get; set; }

        /// <summary>
        /// 回传json
        /// </summary>
        internal string CallbackJson { get; set; }

        /// <summary>
        /// 处理器类型
        /// </summary>
        internal Type HandlerType { get; set; }

        /// <summary>
        /// 异常错误
        /// </summary>
        internal Exception Error { get; set; }
    }
}
