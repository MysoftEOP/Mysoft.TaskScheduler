using System;
using System.Collections.Generic;
using System.Text;

namespace Mysoft.TaskScheduler
{
    public interface ITask
    {
        string Name { get; }

        void Do();

        void Undo();
    }

    public interface ITask<TModel> : ITask
    {
        void Do(TModel model);

        void Undo(TModel model);
    }
}
