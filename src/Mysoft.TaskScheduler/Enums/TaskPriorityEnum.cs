using System;
using System.Collections.Generic;
using System.Text;

namespace Mysoft.TaskScheduler.Enums
{
    /// <summary>
    /// 任务优先级枚举
    /// 按照数值从大到小的顺序优先执行
    /// </summary>
    public enum TaskPriorityEnum
    {
        /// <summary>
        /// 默认
        /// </summary>
        Default = 0,

        /// <summary>
        /// 一级
        /// </summary>
        LevelOne = 1,

        /// <summary>
        /// 二级
        /// </summary>
        LevelTwo = 2,

        /// <summary>
        /// 三级
        /// </summary>
        LevelThree = 3
    }
}
