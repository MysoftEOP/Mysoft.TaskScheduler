using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.States;
using System.Threading;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mysoft.TaskScheduler.Hangfire;
using Mysoft.TaskScheduler.Enums;
using Mysoft.TaskScheduler.Handler;
using Polly;
using System.Linq.Expressions;
using Hangfire.Logging;

namespace Mysoft.TaskScheduler
{
    /// <summary>
    /// 任务管理器
    /// </summary>
    internal sealed class TaskContext
    {
        IServiceProvider _serviceProvider;

        private static readonly ILog Logger = LogProvider.For<TaskContext>();

        internal TaskContext(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 来自Hangfire任务状态改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void TaskStateChanged(object sender, Func<TaskStateChangeEventArgs> func)
        {
            var e = func();

            if (Enum.TryParse(e.StateName, out TaskStateEnum tmpState))
            {
                switch (tmpState)
                {
                    case TaskStateEnum.Succeeded:
                        if (e.ExecutionType == TaskExecutionTypeEnum.Do)
                        {
                            //串行的回滚任务也是挨个执行.
                            //如果移除头部回滚任务,挨个依赖的回滚任务会被hangfire依次删除,所以这里需要判断,最后一个执行任务成功后,移除第一个回滚任务即可

                            if (e.TaskDoIdChain == null || e.TaskDoIdChain.Count == 0)
                            {
                                Logger.Error("未找到执行任务Id串");
                                return;
                            }

                            Logger.Info($"【{e.TaskName}】执行成功 --- TaskDoIdChain:{string.Join(",", e.TaskDoIdChain)}");

                            var lastDoId = e.TaskDoIdChain.LastOrDefault();

                            if (e.Id.Equals(lastDoId, StringComparison.OrdinalIgnoreCase))
                            {
                                if (e.TaskUndoIdChain == null || e.TaskUndoIdChain.Count == 0)
                                {
                                    Logger.Error($"未找到执行回滚任务Id串");
                                    return;
                                }

                                var deleteResult = Delete(e.TaskUndoIdChain.FirstOrDefault());
                                Logger.Info($"删除回滚任务结果:{deleteResult}");
                            }
                        }
                        break;
                    case TaskStateEnum.Failed:
                        Logger.Info($"【{e.TaskName}】执行失败");
                        break;
                    default:
                        return;
                }
            }

            NotifyExecutionHandler(e);
        }

        /// <summary>
        /// 删除任务
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        internal bool Delete(string taskId)
        {
            return BackgroundJob.Delete(taskId);
        }

        private void NotifyExecutionHandler(TaskStateChangeEventArgs e)
        {
            if (e.HandlerType == null)
            {
                Logger.Info("没有回调,直接退出");
                return;
            }

            if (Enum.TryParse(e.StateName, out TaskStateEnum tmpState) == false)
            {
                return;
            }

            Task.Factory.StartNew(async () =>
            {
                var service = _serviceProvider.GetService(e.HandlerType);

                var modelType = e.HandlerType.GetInterfaces().FirstOrDefault(x => x.Name.Contains(nameof(ITaskHandler))).GetGenericArguments()[0];

                MethodInfo method = null;

                Task result = null;

                switch (tmpState)
                {
                    case TaskStateEnum.Succeeded:
                        method = e.HandlerType.GetMethod(nameof(ITaskHandler.DoSuccess));
                        result = (Task)method.Invoke(service, new object[] { Newtonsoft.Json.JsonConvert.DeserializeObject(e.CallbackJson, modelType) });
                        await result;

                        if (TaskFinished(e.Id))
                        {
                            var methodFinished = e.HandlerType.GetMethod(nameof(ITaskHandler.DoFinished));
                            result = (Task)methodFinished.Invoke(service, new object[] { Newtonsoft.Json.JsonConvert.DeserializeObject(e.CallbackJson, modelType) });
                            await result;
                        }
                        break;
                    case TaskStateEnum.Failed:
                        method = e.HandlerType.GetMethod(nameof(ITaskHandler.DoFailed));
                        result = (Task)method.Invoke(service, new object[] { Newtonsoft.Json.JsonConvert.DeserializeObject(e.CallbackJson, modelType), e.Error });
                        await result;
                        break;
                    default:
                        return;
                }
            });
        }

        /// <summary>
        /// 判断任务及其关联任务是否完成
        /// </summary>
        /// <param name="taskId">任务Id</param>
        /// <param name="withRelated">指定查询单个任务或是任务链</param>
        /// <returns></returns>
        public bool TaskFinished(string taskId, bool withRelated = true)
        {
            if (withRelated == false)
            {
                return JobStorage.Current.GetConnection().GetJobData(taskId).State == SucceededState.StateName;
            }

            var doIds = JobStorage.Current.GetConnection().GetJobParameter(taskId, Consts.TASK_DO_CHAIN);

            if (string.IsNullOrWhiteSpace(doIds))
            {
                return TaskFinished(taskId, false);
            }

            var ids = doIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            //判断是否查询最后一个Id
            var lastId = ids.LastOrDefault();

            //如果是 直接查询其完成状态
            if (taskId.Equals(lastId, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Info("检查任务是否全部完成");

                var policyBuilder = Policy
                    .HandleResult<bool>(b =>
                    {
                        return b == false;
                    })
                    .WaitAndRetry(10, i =>
                    {
                        return TimeSpan.FromSeconds(2);
                    }, (r, ts, index, contenxt) =>
                    {
                        Logger.Info("------------------------------------------------------------------");

                        Logger.Info($"第{index}次执行");

                        Logger.Info($"返回结果:{r.Result}");

                        Logger.Info("------------------------------------------------------------------");
                    });

                var result = policyBuilder.Execute(() =>
                {
                    return TaskFinished(lastId, false);
                });

                Logger.Info($"检查任务是否全部完成结果:{result}");

                return result;
            }

            return ids.All(x => TaskFinished(x, false));
        }

        /// <summary>
        /// 等待前置任务执行完毕后执行
        /// </summary>
        /// <param name="parentTaskId">前置任务Id</param>
        /// <param name="caller"></param>
        /// <returns></returns>
        internal static string ContinueWith<T>(string parentTaskId, Expression<Action<T>> caller)
        {
            return BackgroundJob.ContinueWith(parentTaskId, caller, JobContinuationOptions.OnlyOnSucceededState);
        }



        /// <summary>
        /// 定期执行任务
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        internal static string Schedule<T>(Expression<Action<T>> caller, TimeSpan delay)
        {
            return BackgroundJob.Schedule(caller, delay);
        }

        /// <summary>
        /// 设置任务参数
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        internal static void SetTaskParameter(string taskId, string name, string value)
        {
            JobStorage.Current.GetConnection().SetJobParameter(taskId, name, value);
        }

        /// <summary>
        /// 重新入队
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        internal static bool Requeue(string taskId)
        {
            return BackgroundJob.Requeue(taskId);
        }
    }
}
