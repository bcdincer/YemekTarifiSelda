using Hangfire.Dashboard;
using Microsoft.Extensions.Hosting;

namespace BackendApi.Infrastructure.Hangfire;

/// <summary>
/// Authorization filter for Hangfire Dashboard
/// In production, implement proper authentication/authorization
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // TODO: Implement proper authorization (check if user is admin)
        // For now, allow in development only
        var httpContext = context.GetHttpContext();
        return httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();
        
        // Example for production:
        // return httpContext.User.IsInRole("Admin");
    }
}

