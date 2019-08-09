using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mysoft.TaskScheduler.Handler
{
    public class DefaultTaskHandler : ITaskHandler<DefaultCallback>
    {
        [Obsolete]
        public virtual Task DoFailed(DefaultCallback model)
        {
            throw new NotImplementedException();
        }

        public virtual Task DoFailed(DefaultCallback model, Exception ex)
        {
            throw new NotImplementedException();
        }

        public virtual Task DoFinished(DefaultCallback model)
        {
            return Task.FromResult<object>(null);
        }

        public virtual Task DoSuccess(DefaultCallback model)
        {
            return Task.FromResult<object>(null);
        }
    }
}
