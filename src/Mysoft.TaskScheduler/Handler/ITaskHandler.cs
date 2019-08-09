using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mysoft.TaskScheduler.Handler
{
    /// <summary>
    /// 任务执行处理器
    /// </summary>
    /// <typeparam name="TCallback">回调参数类型</typeparam>
    public interface ITaskHandler<TCallback> where TCallback : new()
    {
        /// <summary>
        /// 执行失败
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Obsolete]
        Task DoFailed(TCallback model);

        /// <summary>
        /// 执行失败
        /// </summary>
        /// <param name="model"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        Task DoFailed(TCallback model, Exception error);

        /// <summary>
        /// 执行成功
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task DoSuccess(TCallback model);

        /// <summary>
        /// 执行完成
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task DoFinished(TCallback model);
    }

    /// <summary>
    /// 任务执行处理器
    /// </summary>
    public interface ITaskHandler : ITaskHandler<DefaultCallback>
    {

    }
}
