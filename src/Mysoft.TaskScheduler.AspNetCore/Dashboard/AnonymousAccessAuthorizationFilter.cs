using Hangfire.Annotations;
using Hangfire.Dashboard;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mysoft.TaskScheduler.AspNetCore.Dashboard
{
    internal class AnonymousAccessAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            return true;
        }
    }
}
