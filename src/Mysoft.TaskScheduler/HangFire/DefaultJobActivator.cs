using Hangfire;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mysoft.TaskScheduler.Hangfire
{
    internal class DefaultJobActivator : JobActivator
    {
        private class DefaultJobActivatorScope : JobActivatorScope
        {
            private readonly DefaultJobActivator _activator;

            public DefaultJobActivatorScope(DefaultJobActivator activator)
            {
                this._activator = activator;
            }

            public override object Resolve(Type type)
            {
                return _activator.ActivateJob(type);
            }

            public override void DisposeScope()
            {
                base.DisposeScope();
            }
        }

        private readonly IServiceProvider _provider;

        internal DefaultJobActivator(IServiceProvider provider)
        {
            this._provider = provider;
        }

        public override object ActivateJob(Type jobType)
        {
            return _provider.GetService(jobType);
        }

        public override JobActivatorScope BeginScope(JobActivatorContext context)
        {
            return new DefaultJobActivatorScope(this);
        }
    }
}
