using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mysoft.TaskScheduler;
using Mysoft.TaskScheduler.AspNetCore.Dashboard;
using System;

namespace Microsoft.AspNetCore.Builder
{
    public static class TaskSchedulerApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseTaskScheduler(this IApplicationBuilder app)
        {
            var services = app.ApplicationServices;

            //必须
            //hangfire通过IGlobalConfiguration设置存储等相关初始类
            var configuration = services.GetRequiredService<IGlobalConfiguration>();

            var options = services.GetRequiredService<TaskSchedulerOptions>();

            if (options.OnlyClient)
            {
                return app;
            }

            app.UseHangfireDashboard(options: new DashboardOptions
            {
                DisplayStorageConnectionString = false,
                //AppPath = null,
                //自动刷新毫秒数
                StatsPollingInterval = 100000,
                Authorization = new Hangfire.Dashboard.IDashboardAuthorizationFilter[] { new AnonymousAccessAuthorizationFilter() }
            });

            //启动
            var server = services.GetRequiredService<BackgroundJobServer>();

            var lifetime = services.GetRequiredService<IApplicationLifetime>();

            var cancellationToken = lifetime.ApplicationStopping;
            cancellationToken.Register(() =>
            {
                server.SendStop();
            });

            cancellationToken = lifetime.ApplicationStopped;
            cancellationToken.Register(() =>
            {
                server.Dispose();
            });

            return app;
        }
    }
}