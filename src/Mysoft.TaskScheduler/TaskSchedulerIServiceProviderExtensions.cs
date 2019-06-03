using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Mysoft.TaskScheduler.Filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mysoft.TaskScheduler
{
    public static class TaskSchedulerIServiceProviderExtensions
    {
        public static IServiceProvider StartTaskScheduler(this IServiceProvider provider)
        {
            provider.GetRequiredService<IGlobalConfiguration>();

            var options = provider.GetRequiredService<TaskSchedulerOptions>();
            if (options.OnlyClient)
            {
                return provider;
            }

            provider.GetRequiredService<BackgroundJobServer>();
            return provider;
        }
    }
}
