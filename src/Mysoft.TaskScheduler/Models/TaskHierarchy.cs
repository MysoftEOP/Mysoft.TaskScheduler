using Hangfire.Logging;
using Mysoft.TaskScheduler.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mysoft.TaskScheduler.Models
{
    /// <summary>
    /// 任务等级模型
    /// 父任务+若干子任务
    /// 多个父任务并行执行
    /// </summary>
    internal class TaskHierarchy
    {
        private static readonly ILog Logger = LogProvider.For<TaskHierarchy>();

        internal TaskHierarchy()
        {
            Subs = new List<TaskHierarchy>();
        }

        private string _name;
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            internal set
            {
                _name = value;
            }
        }

        /// <summary>
        /// 父任务
        /// </summary>
        [JsonIgnore]
        public TaskHierarchy Parent { get; internal set; }

        /// <summary>
        /// 前一个任务
        /// </summary>
        [JsonIgnore]
        public TaskHierarchy Previous { get; internal set; }

        /// <summary>
        /// 下一个任务
        /// </summary>
        [JsonIgnore]
        public TaskHierarchy Next { get; internal set; }

        /// <summary>
        /// Id
        /// </summary>
        public TaskIdentity Identity { get; internal set; }

        /// <summary>
        /// 获取父任务Id执行器
        /// </summary>
        [JsonIgnore]
        internal Func<TaskIdentity> IdentityBuilder { get; set; }

        /// <summary>
        /// 运行模式
        /// </summary>
        internal TaskRunningModeEnum Mode { get; set; }

        /// <summary>
        /// 子任务
        /// </summary>
        public List<TaskHierarchy> Subs { get; internal set; }

        /// <summary>
        /// 新增子任务
        /// </summary>
        /// <param name="subTask"></param>
        internal void AddSubTask(TaskHierarchy subTask)
        {
            if (subTask == null)
            {
                throw new ArgumentNullException("subTask");
            }

            //指定子任务的父节点
            subTask.Parent = this;

            if (Subs.Count == 0)
            {
                this.Next = subTask;
            }
            else
            {
                Subs.LastOrDefault().Next = subTask;
            }

            this.Subs.Add(subTask);
        }

        /// <summary>
        /// 生成标识
        /// </summary>
        internal void GenerateIdentity()
        {
            if (Identity != null)
            {
                return;
            }

            if (IdentityBuilder == null)
            {
                return;
            }

            this.Identity = IdentityBuilder();

            //设定执行任务名称
            TaskContext.SetTaskParameter(Identity.DoId, Consts.TASK_NAME, Name);

            //设定执行任务类型
            TaskContext.SetTaskParameter(Identity.DoId, Consts.TASK_EXECUTION_TYPE, (int)TaskExecutionTypeEnum.Do + "");

            //设定执行任务关联撤销任务Id
            TaskContext.SetTaskParameter(Identity.DoId, Consts.TASK_UNDO_ID, Identity.UndoId);

            //设定撤销任务名称
            TaskContext.SetTaskParameter(Identity.UndoId, Consts.TASK_NAME, $"{Name}-回滚");

            //设定执行任务类型
            TaskContext.SetTaskParameter(Identity.UndoId, Consts.TASK_EXECUTION_TYPE, (int)TaskExecutionTypeEnum.Undo + "");
        }

        /// <summary>
        /// 获取任务的所有标识
        /// </summary>
        /// <param name="identitySelector">标识筛选器(执行任务或回滚任务)</param>
        /// <param name="includeSelf">是否包含对象本身</param>
        /// <returns></returns>
        internal List<string> GetTaskIdentities(Func<TaskIdentity, string> identitySelector = null, bool includeSelf = true)
        {
            var ids = new List<string>();

            SearchTaskId(includeSelf ? this : this.Next, ids, identitySelector);
            return ids;
        }

        /// <summary>
        /// 获取所有任务的标识
        /// </summary>
        /// <param name="identitySelector">标识筛选器(执行任务或回滚任务)</param>
        /// <returns></returns>
        internal List<string> GetAllTaskIdentities(Func<TaskIdentity, string> identitySelector = null)
        {
            var ids = new List<string>();
            SearchTaskId(this.Parent ?? this, ids, identitySelector);
            return ids;
        }

        /// <summary>
        /// 递归搜索任务Id
        /// </summary>
        /// <param name="task">任务层级对象</param>
        /// <param name="ids">Id集合</param>
        /// <param name="identitySelector">标识筛选器(执行任务或回滚任务)</param>
        private void SearchTaskId(TaskHierarchy task, List<string> ids, Func<TaskIdentity, string> identitySelector)
        {
            if (task == null)
            {
                return;
            }

            var selector = new Func<TaskIdentity, string>(x => x.DoId);
            if (identitySelector != null)
            {
                selector = identitySelector;
            }

            ids.Add(selector(task.Identity));
            SearchTaskId(task.Next, ids, selector);
        }

        /// <summary>
        /// 设置任务链
        /// 任务链以主任务Id与各子任务Id合并,按逗号分隔的格式保存
        /// </summary>
        internal void SetTaskChain(string callbackJson, Type handlerType)
        {
            if (Parent != null)
            {
                return;
            }
            try
            {
                var backJson = callbackJson ?? "";

                ////获取执行或回滚任务链时可能会存在任务标识还未被创建的问题,所以这里需要等待处理
                //var retryResult = Policy
                //    .HandleResult<bool>(x => x)
                //    .WaitAndRetry(10, x =>
                //    {
                //        Logger.Warn($"第[{x}]次重试获取执行或回滚任务链");
                //        return TimeSpan.FromSeconds(2);
                //    })
                //    .Execute(() =>
                //    {
                //        return this.Subs?.Any(x => string.IsNullOrWhiteSpace(x.Identity?.DoId) == true || string.IsNullOrWhiteSpace(x.Identity?.UndoId) == true) == true;
                //    })
                //    ;

                //if (retryResult)
                //{
                //    Logger.Error("重试完毕,未获取到正确的执行或回滚任务链数据");
                //}

                var taskDoChain = string.Join(",", GetAllTaskIdentities(x => x.DoId));

                Logger.Info($"执行Id链:{taskDoChain}");

                var taskUndoChain = string.Join(",", GetAllTaskIdentities(x => x.UndoId));

                Logger.Info($"回滚Id链:{taskUndoChain}");

                TaskContext.SetTaskParameter(Identity.DoId, Consts.TASK_DO_CHAIN, taskDoChain);

                TaskContext.SetTaskParameter(Identity.DoId, Consts.TASK_UNDO_CHAIN, taskUndoChain);

                TaskContext.SetTaskParameter(Identity.DoId, Consts.TASK_CALL_BACK, backJson);

                TaskContext.SetTaskParameter(Identity.DoId, Consts.TASK_HANDLER_TYPE, handlerType == null ? "" : handlerType.AssemblyQualifiedName);

                Subs.ForEach(x =>
                {
                    TaskContext.SetTaskParameter(x.Identity.DoId, Consts.TASK_DO_CHAIN, taskDoChain);
                    TaskContext.SetTaskParameter(x.Identity.DoId, Consts.TASK_UNDO_CHAIN, taskUndoChain);
                    TaskContext.SetTaskParameter(x.Identity.DoId, Consts.TASK_CALL_BACK, backJson);
                    TaskContext.SetTaskParameter(x.Identity.DoId, Consts.TASK_HANDLER_TYPE, handlerType == null ? "" : handlerType.AssemblyQualifiedName);
                });

                //所有参数设置完毕后,第一个任务才入队开始执行
                var firstDoId = taskDoChain.Split(new char[] { ',' }).FirstOrDefault();

                TaskContext.Requeue(firstDoId);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("SetTaskChain异常", ex);
                throw ex;
            }
        }
    }
}
