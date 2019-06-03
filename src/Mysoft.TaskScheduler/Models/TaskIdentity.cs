using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mysoft.TaskScheduler.Models
{
    /// <summary>
    /// 任务标识对象模型
    /// </summary>
    [Serializable]
    internal class TaskIdentity
    {
        /// <summary>
        /// 执行任务Id
        /// </summary>
        [JsonProperty]
        public string DoId { get; internal set; }

        /// <summary>
        /// 回滚任务Id
        /// </summary>
        [JsonProperty]
        public string UndoId { get; internal set; }
    }
}
