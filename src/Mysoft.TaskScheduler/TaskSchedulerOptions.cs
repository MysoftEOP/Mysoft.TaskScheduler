using Microsoft.Extensions.DependencyInjection;
using Mysoft.TaskScheduler.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Mysoft.TaskScheduler
{
    /// <summary>
    /// 调度任务程序配置
    /// </summary>
    public class TaskSchedulerOptions
    {
        public TaskSchedulerOptions()
        {
            WorkerCount = 2;
            RetryCount = 2;
            RedisDbIndex = 10;
            EnableConsoleLog = false;
            BuilderLifetime = ServiceLifetime.Transient;
        }

        /// <summary>
        /// 工作线程
        /// </summary>
        public int WorkerCount { get; set; }

        /// <summary>
        /// 遇错重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// redis 连接字符串
        /// </summary>
        public string RedisConnectionStrings { get; set; }

        /// <summary>
        /// Redis Db索引
        /// </summary>
        public int RedisDbIndex { get; set; }

        /// <summary>
        /// Redis 前缀
        /// </summary>
        public string RedisPrefix { get; set; }

        /// <summary>
        /// 队列名称
        /// </summary>
        internal string[] Queues
        {
            get
            {
                var priorities = Enum.GetValues(typeof(TaskPriorityEnum)).Cast<TaskPriorityEnum>();
                var queues = priorities.OrderByDescending(x => (int)x).Select(x => x.ToString().ToLower()).ToArray();
                return queues;
            }
        }

        /// <summary>
        /// 启用控制台输出
        /// </summary>
        public bool EnableConsoleLog { get; set; }

        /// <summary>
        /// 服务器名称
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// 仅加载客户端
        /// </summary>
        public bool OnlyClient { get; set; }

        /// <summary>
        /// 构造器的生命周期
        /// </summary>
        public ServiceLifetime BuilderLifetime { get; set; }

        internal static TaskSchedulerOptions Default
        {
            get
            {
                var options = new TaskSchedulerOptions { };
                return options;
            }
        }
    }
}
