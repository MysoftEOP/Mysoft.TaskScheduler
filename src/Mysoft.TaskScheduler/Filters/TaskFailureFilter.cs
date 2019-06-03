using Hangfire.Common;
using Hangfire.States;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Hangfire;
using Mysoft.TaskScheduler.Enums;
using Mysoft.TaskScheduler.Hangfire;

namespace Mysoft.TaskScheduler.Filters
{
    /// <summary>
    /// 任务失败筛选器
    /// </summary>
    internal class TaskFailureFilter : JobFilterAttribute, IElectStateFilter
    {
        private readonly TaskSchedulerOptions _options;

        public TaskFailureFilter(TaskSchedulerOptions options)
        {
            _options = options;
        }

        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is FailedState failedState)
            {
                var executionType = (TaskExecutionTypeEnum)context.GetJobParameter<int>(Consts.TASK_EXECUTION_TYPE);

                if (executionType == TaskExecutionTypeEnum.Do)
                {
                    //判断Do任务是否超过重试次数
                    var retryCount = context.GetJobParameter<int>("RetryCount");
                    if (retryCount >= _options.RetryCount)
                    {
                        //context.GetJobParameter<T>方法无法解析非int类型数据
                        //超过就执行Undo任务
                        var undoChain = context.Connection.GetJobParameter(context.BackgroundJob.Id, Consts.TASK_UNDO_CHAIN);
                        if (string.IsNullOrWhiteSpace(undoChain) == false)
                        {
                            var firstUndoId = undoChain.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                            if (string.IsNullOrWhiteSpace(firstUndoId) == false)
                            {
                                BackgroundJob.Requeue(firstUndoId);
                            }
                        }

                        //标记所有非当前的Do任务状态为已删除
                        //当前任务已被直接更新为failed,无需处理
                        var doChain = context.Connection.GetJobParameter(context.BackgroundJob.Id, Consts.TASK_DO_CHAIN);
                        if (string.IsNullOrWhiteSpace(doChain) == false)
                        {
                            foreach (var doId in doChain.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Equals(context.BackgroundJob.Id) == false))
                            {
                                BackgroundJob.Delete(doId);
                            }
                        }
                    }
                }
            }
        }
    }
}
