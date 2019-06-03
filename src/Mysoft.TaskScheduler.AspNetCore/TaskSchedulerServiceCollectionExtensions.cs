using Castle.MicroKernel.Registration;
using Castle.Windsor.Installer;
using Hangfire.Dashboard;
using Mysoft.TaskScheduler;
using System;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class TaskSchedulerServiceCollectionExtensions
    {
        public static IServiceCollection AddTaskSchedulerDashboard(this IServiceCollection services, TaskSchedulerOptions options = null)
        {
            services
                .AddTaskScheduler(options)
                .AddSingleton(x => DashboardRoutes.Routes)
                ;

            return services;
        }
    }
}

namespace Castle.Windsor
{
    public static partial class TaskSchedulerServiceCollectionExtensions
    {
        public static IWindsorContainer AddTaskSchedulerDashboard(this IWindsorContainer container, TaskSchedulerOptions options = null)
        {
            container
                .AddTaskScheduler(options)
                .Register(
                    Component
                        .For<RouteCollection>()
                        .Instance(DashboardRoutes.Routes))
                        ;

            return container;
        }
    }
}