using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor.MsDependencyInjection;
using Hangfire;
using Hangfire.Common;
using Hangfire.Redis;
using Mysoft.TaskScheduler;
using Mysoft.TaskScheduler.Filters;
using Mysoft.TaskScheduler.Handler;
using Mysoft.TaskScheduler.Hangfire;
using System;
using System.Collections.Generic;
using System.Text;

namespace Castle.Windsor
{
    public class WindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            var options = container.Resolve<TaskSchedulerOptions>();

            container.Register(
              Component
                  .For<JobStorage>()
                  .Instance(new RedisStorage(
                      options.RedisConnectionStrings,
                      new RedisStorageOptions
                      {
                          Db = options.RedisDbIndex,
                          Prefix = string.IsNullOrWhiteSpace(options.RedisPrefix) ? RedisStorageOptions.DefaultPrefix : string.Format(ApplicationStrings.REDIS_PRIFIX_FORMAT, options.RedisPrefix)
                      }))
                  .LifestyleSingleton())
                  ;

            container.Register(
                Component
                    .For<JobActivator>()
                    .UsingFactoryMethod(x =>
                    {
                        return new DefaultJobActivator(x.Resolve<IServiceProvider>());
                    })
                    //.ImplementedBy<DefaultJobActivator>()
                    .LifestyleSingleton())
                    ;

            container.Register(
                Component
                    .For<TaskContext>()
                    .UsingFactoryMethod(x =>
                    {
                        return new TaskContext(x.Resolve<IServiceProvider>());
                    })
                    //.ImplementedBy<TaskContext>()
                    .LifestyleSingleton())
                    ;

            container.Register(
                Component
                    .For<IGlobalConfiguration>()
                    .UsingFactoryMethod(x =>
                    {
                        var activator = x.Resolve<JobActivator>();
                        var context = x.Resolve<TaskContext>();
                        var jobStorage = x.Resolve<JobStorage>();

                        JobFilterProviders.Providers.Add(new TaskQueueFilterProvider(options));

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
                    .LifestyleSingleton());

            switch (options.BuilderLifetime)
            {
                case Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped:
                    container.Register(
                        Component
                            .For<ITaskBuilder>()
                            .ImplementedBy<TaskBuilder>()
                            .LifestyleCustom<MsScopedLifestyleManager>()
                            .OnDestroy((kernel, instance) =>
                            {
                                if (instance.GetRootTasksCount() > 0)
                                {
                                    instance.Build();
                                }
                            }));
                    break;
                case Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton:
                    container.Register(
                       Component
                           .For<ITaskBuilder>()
                           .ImplementedBy<TaskBuilder>()
                           .LifestyleSingleton());
                    break;
                case Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient:
                    container.Register(
                       Component
                           .For<ITaskBuilder>()
                           .ImplementedBy<TaskBuilder>()
                           .LifestyleTransient());
                    break;
            }

            container.Register(
                Types
                    .FromAssemblyInDirectory(new AssemblyFilter(AppContext.BaseDirectory))
                    .IncludeNonPublicTypes()
                    .BasedOn<ITask>()
                    .If(x => x.IsAbstract == false)
                    .WithServiceSelf()
                    .WithServiceAllInterfaces()
                    .LifestyleTransient())
                    ;

            container.Register(
                Types
                    .FromAssemblyInDirectory(new AssemblyFilter(AppContext.BaseDirectory))
                    .IncludeNonPublicTypes()
                    .BasedOn(typeof(ITaskHandler<>))
                    .If(x => x.IsAbstract == false)
                    .WithServiceSelf()
                    .WithServiceAllInterfaces()
                    .LifestyleTransient())
                    ;


            if (options.OnlyClient)
            {
                return;
            }

            container.Register(
                Component
                    .For<BackgroundJobServerOptions>()
                    .Instance(new BackgroundJobServerOptions()
                    {
                        WorkerCount = options.WorkerCount,
                        Queues = options.Queues,
                        ServerName = options.ServerName
                    })
                    .LifestyleSingleton());

            container.Register(
                Component
                    .For<BackgroundJobServer>()
                    .UsingFactoryMethod(x =>
                    {
                        x.Resolve<IGlobalConfiguration>();
                        var config = x.Resolve<BackgroundJobServerOptions>();
                        return new BackgroundJobServer(config);
                    })
                    .OnDestroy(x =>
                    {
                        x.SendStop();
                    })
                    .LifestyleSingleton());
        }
    }
}
