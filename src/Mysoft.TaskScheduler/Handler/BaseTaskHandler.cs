using Hangfire.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mysoft.TaskScheduler.Handler
{
    /// <summary>
    /// 默认任务执行处理器
    /// </summary>
    /// <typeparam name="TCallback">回调参数类型</typeparam>
    public abstract class BaseTaskHandler<TCallback> : ITaskHandler<TCallback> where TCallback : new()
    {
        private static readonly ILog Logger = LogProvider.For<BaseTaskHandler<TCallback>>();

        /// <summary>
        /// 执行失败
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public virtual Task DoFailed(TCallback model)
        {
            Logger.Error($"DoFailed --- type is {typeof(TCallback).AssemblyQualifiedName} data is {Newtonsoft.Json.JsonConvert.SerializeObject(model)}");
            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// 执行完成
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public virtual Task DoFinished(TCallback model)
        {
            Logger.Debug($"DoFinished --- type is {typeof(TCallback).AssemblyQualifiedName} data is {Newtonsoft.Json.JsonConvert.SerializeObject(model)}");
            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// 执行成功
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public virtual Task DoSuccess(TCallback model)
        {
            Logger.Debug($"DoSuccess --- type is {typeof(TCallback).AssemblyQualifiedName} data is {Newtonsoft.Json.JsonConvert.SerializeObject(model)}");
            return Task.FromResult<object>(null);
        }
    }
}
