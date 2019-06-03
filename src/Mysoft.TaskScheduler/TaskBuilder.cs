using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using Hangfire;
using Newtonsoft.Json;
using Mysoft.TaskScheduler.Models;
using Mysoft.TaskScheduler.Hangfire;
using Mysoft.TaskScheduler.Enums;
using Mysoft.TaskScheduler.Handler;
using HF = Hangfire;
using Hangfire.Logging;

namespace Mysoft.TaskScheduler
{
    /// <summary>
    /// 任务构造器
    /// </summary>
    public sealed class TaskBuilder : ITaskBuilder
    {
        private static readonly ILog Logger = LogProvider.For<TaskBuilder>();

        public TaskBuilder()
        {
            RootTasks = new List<TaskHierarchy>();
            CallbackJson = "";
            TaskHandlerType = null;
        }

        #region "属性"
        /// <summary>
        /// 根任务调度列表
        /// </summary>
        private List<TaskHierarchy> RootTasks { get; set; }

        /// <summary>
        /// 回发json数据
        /// </summary>
        private string CallbackJson { get; set; }

        /// <summary>
        /// 回发handler的类型
        /// </summary>
        private Type TaskHandlerType { get; set; }
        #endregion

        #region "方法"

        #region "入队"
        /// <summary>
        /// 入队，暂时仅支持串行
        /// </summary>
        /// <typeparam name="TTask">任务类型</typeparam>
        /// <returns></returns>
        public ITaskBuilder Enqueue<TTask>() where TTask : ITask
        {
            if (RootTasks.Count == 0)
            {
                return Apply<TTask>();
            }

            return Continue<TTask>();
        }

        /// <summary>
        /// 入队，暂时仅支持串行
        /// </summary>
        /// <typeparam name="TTask">任务类型</typeparam>
        /// <typeparam name="TModel">任务参数类型</typeparam>
        /// <param name="model">任务参数</param>
        /// <returns></returns>
        public ITaskBuilder Enqueue<TTask, TModel>(TModel model)
            where TTask : ITask<TModel>
            where TModel : new()
        {
            if (RootTasks.Count == 0)
            {
                return Apply<TTask, TModel>(model);
            }

            return Continue<TTask, TModel>(model);
        }
        #endregion

        #region "并行"
        /// <summary>
        /// 创建任务
        /// </summary>
        /// <typeparam name="TTask">任务类型</typeparam>
        /// <param name="doMethod">任务执行方法</param>
        /// <param name="undoMethod">任务回滚方法</param>
        /// <returns></returns>
        private ITaskBuilder Apply<TTask>(Expression<Action<TTask>> doMethod, Expression<Action<TTask>> undoMethod)
            where TTask : ITask
        {
            Logger.Info($"Begin Apply RootTasks Count:{RootTasks.Count}");

            RootTasks.Add(new TaskHierarchy
            {
                IdentityBuilder = () =>
                {
                    //延迟执行
                    //所有任务设定参数完毕才执行第一个任务
                    //MysoftTaskHierarchy.SetTaskChain方法中设定

                    var doId = TaskContext.Schedule(doMethod, TimeSpan.FromDays(1000));

                    //撤销任务入队
                    //延迟执行
                    var undoId = TaskContext.Schedule(undoMethod, TimeSpan.FromDays(1000));

                    return new TaskIdentity
                    {
                        DoId = doId,
                        UndoId = undoId
                    };
                },
                Mode = TaskRunningModeEnum.Parallel,
                Name = GetTaskName<TTask>()
            });

            Logger.Info($"After Apply RootTasks Count:{RootTasks.Count}");

            return this;
        }

        /// <summary>
        /// 创建任务
        /// </summary>
        /// <typeparam name="TTask">任务类型</typeparam>
        /// <typeparam name="TModel">任务参数类型</typeparam>
        /// <param name="model">任务参数</param>
        /// <returns></returns>
        private ITaskBuilder Apply<TTask, TModel>(TModel model)
            where TTask : ITask<TModel>
            where TModel : new()
        {
            return this.Apply<TTask>(x => x.Do(model), x => x.Undo(model));
        }

        /// <summary>
        /// 创建任务
        /// </summary>
        /// <typeparam name="TTask">任务类型</typeparam>
        /// <returns></returns>
        private ITaskBuilder Apply<TTask>()
            where TTask : ITask
        {
            return this.Apply<TTask>(x => x.Do(), x => x.Undo());
        }
        #endregion

        #region "串行"
        /// <summary>
        /// 任务追加
        /// </summary>
        /// <typeparam name="TTask">任务类型</typeparam>
        /// <param name="doMethod">任务执行方法</param>
        /// <param name="undoMethod">任务回滚方法</param>
        /// <returns></returns>
        private ITaskBuilder Continue<TTask>(Expression<Action<TTask>> doMethod, Expression<Action<TTask>> undoMethod)
            where TTask : ITask
        {
            Logger.Info($"Begin Continue RootTasks Count:{RootTasks.Count}");

            var last = this.RootTasks.LastOrDefault();

            if (last == null)
            {
                throw new ArgumentNullException("未找到前置任务");
            }

            var parent = last.Mode == TaskRunningModeEnum.Parallel ? last : last.Parent;

            var previous = last.Subs.Count == 0 ? last : last.Subs.LastOrDefault();

            parent.AddSubTask(new TaskHierarchy
            {
                IdentityBuilder = () =>
                {
                    previous.GenerateIdentity();

                    //执行任务入队
                    var doId = TaskContext.ContinueWith(previous.Identity.DoId, doMethod);

                    //撤销任务入队
                    //延迟执行
                    var undoId = TaskContext.ContinueWith(previous.Identity.UndoId, undoMethod);

                    return new TaskIdentity
                    {
                        DoId = doId,
                        UndoId = undoId
                    };
                },
                Mode = TaskRunningModeEnum.Serial,
                Name = GetTaskName<TTask>(),
                Previous = previous
            });

            Logger.Info($"After Continue RootTasks Count:{RootTasks.Count}");

            return this;
        }

        /// <summary>
        /// 任务追加
        /// </summary>
        /// <typeparam name="TTask">任务类型</typeparam>
        /// <typeparam name="TModel">任务参数类型</typeparam>
        /// <param name="model">任务参数</param>
        /// <returns></returns>
        private ITaskBuilder Continue<TTask, TModel>(TModel model)
            where TTask : ITask<TModel>
            where TModel : new()
        {
            return this.Continue<TTask>(x => x.Do(model), x => x.Undo(model));
        }

        /// <summary>
        /// 任务追加
        /// </summary>
        /// <typeparam name="TTask">任务类型</typeparam>
        /// <returns></returns>
        private ITaskBuilder Continue<TTask>()
         where TTask : ITask
        {
            return this.Continue<TTask>(x => x.Do(), x => x.Undo());
        }
        #endregion

        #region "构建队列"
        /// <summary>
        /// 构造
        /// </summary>
        /// <returns>返回任务Id列表</returns>
        public void Build()
        {
            if (RootTasks.Count == 0)
            {
                Logger.Error("构造失败:未找到任务信息");
                return;
            }

            Logger.Info($"RootTasks Count:{RootTasks.Count}");

            //延迟执行每个父任务
            //依次创建任务
            //从foreach改为for循环
            for (var i = 0; i < RootTasks.Count; i++)
            {
                if (RootTasks.Count <= i)
                {
                    Logger.Info($"RootTasks Count:{RootTasks.Count}");
                    Logger.Error("RootTasks超出索引");
                    break;
                }

                var task = RootTasks[i];

                //判断是否仅有父任务
                if (task.Subs.Count == 0)
                {
                    task.GenerateIdentity();
                }

                //否则执行父任务的最后一个子任务
                else
                {
                    task.Subs.LastOrDefault().GenerateIdentity();
                }

                task.SetTaskChain(CallbackJson, TaskHandlerType);
            }

            //清空任务集合
            //RootTasks.Clear();

            Logger.Info("队列构造完毕,准备清空");
            Clear();
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        public void Build<THandler>() where THandler : ITaskHandler<DefaultCallback>
        {
            Build<DefaultCallback, THandler>(new DefaultCallback());
        }

        public void Build<TCallback, THandler>(TCallback callback = default(TCallback))
            where TCallback : new()
            where THandler : ITaskHandler<TCallback>
        {
            AttachCallback<TCallback, THandler>(callback);
            Build();
        }
        #endregion

        /// <summary>
        /// 获取任务名称
        /// </summary>
        /// <typeparam name="TTask">任务类型</typeparam>
        /// <param name="doMethod"></param>
        /// <returns></returns>
        private string GetTaskName<TTask>()
            where TTask : ITask
        {
            if (!(JobActivator.Current.ActivateJob(typeof(TTask)) is ITask tmp))
            {
                throw new ArgumentNullException("Task");
            }

            if (string.IsNullOrWhiteSpace(tmp.Name))
            {
                throw new ArgumentNullException("Name", "未设置任务名称");
            }

            return tmp.Name;
        }

        /// <summary>
        /// 父任务个数
        /// </summary>
        /// <returns></returns>
        public int GetRootTasksCount()
        {
            return RootTasks.Count;
        }

        /// <summary>
        /// 清理队列
        /// </summary>
        public void Clear()
        {
            Logger.Info("队列已清空");
            RootTasks.Clear();
            CallbackJson = "";
            TaskHandlerType = null;
        }

        public void AttachCallback<TCallback, THandler>(TCallback callback = default(TCallback))
        {
            CallbackJson = callback == null ? "" : JsonConvert.SerializeObject(callback);
            TaskHandlerType = typeof(THandler);
        }

        public void AttachCallback<THandler>() where THandler : ITaskHandler<DefaultCallback>
        {
            AttachCallback<DefaultCallback, THandler>();
        }
        #endregion
    }
}
