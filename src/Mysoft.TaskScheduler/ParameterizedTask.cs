using System;
using System.Collections.Generic;
using System.Text;

namespace Mysoft.TaskScheduler
{
    public class ParameterizedTask<TModel> : ITask<TModel>
    {
        public virtual string Name => "带参数任务";

        public virtual void Do(TModel model) { }

        public void Do()
        {
            throw new ArgumentNullException("不允许调用无参数方法", innerException: null);
        }

        public virtual void Undo(TModel model) { }

        public void Undo()
        {
            throw new ArgumentNullException("不允许调用无参数方法", innerException: null);
        }
    }
}
