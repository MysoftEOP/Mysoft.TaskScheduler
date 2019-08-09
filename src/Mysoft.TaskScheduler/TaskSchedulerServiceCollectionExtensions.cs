using Hangfire;
using Hangfire.Redis;
using Mysoft.TaskScheduler;
using Mysoft.TaskScheduler.Filters;
using Mysoft.TaskScheduler.Hangfire;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using Castle.MicroKernel.Registration;
using Castle.Windsor.Installer;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TaskSchedulerServiceCollectionExtensions
    {
        public static IServiceCollection AddTaskScheduler(this IServiceCollection services, TaskSchedulerOptions options = null)
        {
            options = options ?? TaskSchedulerOptions.Default;

            services
                .AddSingleton(options)
                .AddSingleton(x =>
                {
                    var activator = x.GetRequiredService<JobActivator>();
                    var context = x.GetRequiredService<TaskContext>();
                    var jobStorage = x.GetRequiredService<JobStorage>();

                    Hangfire.Common.JobFilterProviders.Providers.Add(new TaskQueueFilterProvider(options));

                    IGlobalConfiguration configuration = GlobalConfiguration.Configuration
                        .UseStorage(jobStorage)
                        .UseFilter(new TaskNotificationFilter(context.TaskStateChanged))
                        .UseFilter(new AutomaticRetryAttribute
                        {
                            Attempts = options.RetryCount
                        })
                        .UseFilter(new TaskFailureFilter(options))
                        .UseActivator(activator)
                       ;

                    if (options.EnableConsoleLog)
                    {
                        configuration
                            //.UseColouredConsoleLogProvider()
                            .UseLog4NetLogProvider()
                            .UseFilter(new TaskLogFilter());
                    }

                    return configuration;
                })
                .AddSingleton<JobStorage>(x =>
                {
                    return new RedisStorage(
                        options.RedisConnectionStrings,
                        new RedisStorageOptions
                        {
                            Db = options.RedisDbIndex,
                            Prefix = string.IsNullOrWhiteSpace(options.RedisPrefix) ? RedisStorageOptions.DefaultPrefix : string.Format(ApplicationStrings.REDIS_PRIFIX_FORMAT, options.RedisPrefix)
                        });
                })
                .AddSingleton(x => new TaskContext(x))
                .AddSingleton<JobActivator, DefaultJobActivator>(x =>
                {
                    return new DefaultJobActivator(x);
                })
                ;

            services.Add(new ServiceDescriptor(typeof(ITaskBuilder), x =>
            {
                if (options.OnlyClient == false)
                {
                    x.GetRequiredService<BackgroundJobServer>();
                }
                else
                {
                    x.GetRequiredService<IGlobalConfiguration>();
                }
                
                return new TaskBuilder();
            }, options.BuilderLifetime));

            if (options.OnlyClient)
            {
                return services;
            }

            services
                .AddSingleton(x =>
                {
                    return new BackgroundJobServerOptions()
                    {
                        WorkerCount = options.WorkerCount,
                        Queues = options.Queues,
                        ServerName = options.ServerName
                    };
                })
                .AddSingleton(x =>
                {
                    x.GetRequiredService<IGlobalConfiguration>();

                    var config = x.GetService<BackgroundJobServerOptions>();
                    return new BackgroundJobServer(config);
                })
            ;

            return services;
        }
    }
}

namespace Castle.Windsor
{
    public static partial class TaskSchedulerServiceCollectionExtensions
    {
        public static IWindsorContainer AddTaskScheduler(this IWindsorContainer container, TaskSchedulerOptions options = null)
        {
            options = options ?? TaskSchedulerOptions.Default;

            container.Register(
                Component
                    .For<TaskSchedulerOptions>()
                    .Instance(options)
                    .LifestyleSingleton());

            container.Install(FromAssembly.Instance(Assembly.GetExecutingAssembly()));

            return container;
        }
    }
}