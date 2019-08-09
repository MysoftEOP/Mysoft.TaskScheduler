using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Mysoft.TaskScheduler.Hangfire;
using Mysoft.TaskScheduler.Enums;

namespace Mysoft.TaskScheduler.Filters
{
    /// <summary>
    /// 通知筛选器
    /// </summary>
    internal class TaskNotificationFilter : JobFilterAttribute, IApplyStateFilter
    {
        private event EventHandler<Func<TaskStateChangeEventArgs>> OnTaskStateChanged;

        internal TaskNotificationFilter(EventHandler<Func<TaskStateChangeEventArgs>> onTaskStateChanged)
        {
            this.OnTaskStateChanged = onTaskStateChanged;
        }

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            //事件通知
            OnTaskStateChanged?.Invoke(context.BackgroundJob, () =>
            {
                var handlerTypeString = context.GetTaskParameter(Consts.TASK_HANDLER_TYPE);


                var ex = context.NewState as FailedState == null ? null : (context.NewState as FailedState).Exception;

                return new TaskStateChangeEventArgs
                {
                    Id = context.BackgroundJob.Id,
                    StateName = context.NewState.Name,
                    UndoTaskId = context.GetTaskParameter(Consts.TASK_UNDO_ID),
                    TaskName = context.GetTaskParameter(Consts.TASK_NAME),
                    TaskDoIdChain = context.GetTaskParameter(Consts.TASK_DO_CHAIN)?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                    TaskUndoIdChain = context.GetTaskParameter(Consts.TASK_UNDO_CHAIN)?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                    ExecutionType = (TaskExecutionTypeEnum)Convert.ToInt32(context.GetTaskParameter(Consts.TASK_EXECUTION_TYPE)),
                    CallbackJson = context.GetTaskParameter(Consts.TASK_CALL_BACK),
                    HandlerType = string.IsNullOrWhiteSpace(handlerTypeString) ? null : Type.GetType(handlerTypeString),
                    Error = ex
                };
            });
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            // Logger.InfoFormat(
            //"Job `{0}` state `{1}` was unapplied.",
            //context.BackgroundJob.Id,
            //context.OldStateName);
        }
    }
}
