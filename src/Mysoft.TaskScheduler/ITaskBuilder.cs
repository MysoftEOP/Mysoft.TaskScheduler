using Mysoft.TaskScheduler.Handler;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Mysoft.TaskScheduler
{
    /// <summary>
    /// 任务构造器
    /// </summary>
    public interface ITaskBuilder
    {
        /// <summary>
        /// 入队，暂时仅支持串行
        /// </summary>
        /// <typeparam name="TTask">任务类型</typeparam>
        /// <returns></returns>
        ITaskBuilder Enqueue<TTask>()
            where TTask : ITask;

        /// <summary>
        /// 入队，暂时仅支持串行
        /// </summary>
        /// <typeparam name="TTask">任务类型</typeparam>
        /// <typeparam name="TModel">任务参数类型</typeparam>
        /// <param name="model">任务参数</param>
        /// <returns></returns>
        ITaskBuilder Enqueue<TTask, TModel>(TModel model)
            where TTask : ITask<TModel>
            where TModel : new();

        /// <summary>
        /// 构建任务
        /// </summary>
        void Build();

        /// <summary>
        /// 构建任务
        /// </summary>
        /// <typeparam name="TCallback">回传数据类型</typeparam>
        /// <param name="callback">回传数据实例</param>
        void Build<THandler>()
            where THandler : ITaskHandler<DefaultCallback>;

        /// <summary>
        /// 构建任务
        /// </summary>
        /// <typeparam name="TCallback">回传数据类型</typeparam>
        /// <typeparam name="THandler">回传Handler类型</typeparam>
        /// <param name="callback">回传数据实例</param>
        void Build<TCallback, THandler>(TCallback callback = default(TCallback))
            where TCallback : new()
            where THandler : ITaskHandler<TCallback>;

        /// <summary>
        /// 附加回传
        /// </summary>
        /// <typeparam name="TCallback">回传数据类型</typeparam>
        /// <typeparam name="THandler">回传Handler类型</typeparam>
        /// <param name="callback"></param>
        void AttachCallback<TCallback, THandler>(TCallback callback = default(TCallback));

        /// <summary>
        /// 附加回传
        /// </summary>
        /// <typeparam name="THandler">回传Handler类型</typeparam>
        void AttachCallback<THandler>()
            where THandler : ITaskHandler<DefaultCallback>;

        /// <summary>
        /// 获取根任务个数
        /// </summary>
        /// <returns></returns>
        int GetRootTasksCount();

        /// <summary>
        /// 清理
        /// </summary>
        void Clear();
    }
}
