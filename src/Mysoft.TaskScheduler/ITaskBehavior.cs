using Mysoft.TaskScheduler.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mysoft.TaskScheduler
{
    /// <summary>
    /// 任务行为
    /// </summary>
    public interface ITaskBehavior
    {
        /// <summary>
        /// 队列优先级
        /// </summary>

        TaskPriorityEnum Priority { get; set; }
    }
}
