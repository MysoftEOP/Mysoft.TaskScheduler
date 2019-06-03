using Hangfire;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mysoft.TaskScheduler.Filters
{
    internal class TaskQueueFilterProvider : IJobFilterProvider
    {
        private readonly TaskSchedulerOptions _options;

        private static readonly ILog Logger = LogProvider.For<TaskLogFilter>();

        internal TaskQueueFilterProvider(TaskSchedulerOptions options)
        {
            this._options = options;
        }

        public IEnumerable<JobFilter> GetFilters(Job job)
        {
            var queueName = GetQueueName(job);

            return new JobFilter[]
            {
                new JobFilter(new QueueAttribute(queueName), JobFilterScope.Method, null)
            };
        }

        /// <summary>
        /// 获取入队名称
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        string GetQueueName(Job job)
        {
            if (job.Args != null && job.Args.Count > 0 && job.Args[0] is ITaskBehavior)
            {
                var behavior = job.Args[0] as ITaskBehavior;

                var queue = behavior.Priority.ToString().ToLower();

                if (string.IsNullOrWhiteSpace(queue) == false)
                {
                    //判断入队的名称是否在全局队列中
                    //存在返回;不存在返回default队列
                    if (_options.Queues.Any(x => x.Equals(queue)))
                    {
                        return queue;
                    }

                    Logger.Warn($"未找到指定队列[{queue}],已切换默认队列,任务信息:{job.ToString()}");
                }
            }

            return EnqueuedState.DefaultQueue;
        }
    }
}
