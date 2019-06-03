using System;
using System.Collections.Generic;
using System.Text;

namespace Mysoft.TaskScheduler
{
    public abstract class SimpleTask : ITask
    {
        public virtual string Name => "默认任务";

        public abstract void Do();

        public abstract void Undo();
    }
}
