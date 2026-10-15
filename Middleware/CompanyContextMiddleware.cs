using ModernWeb.Services;

namespace ModernWeb.Middleware;

/// <summary>
/// Loads the Redis-backed session/company context for the signed-in user and makes it
/// available to controllers as HttpContext.Items["Company"]. Runs after authentication,
/// so it only does work for requests that already passed the JWT check.
/// </summary>
public class CompanyContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, SessionClient sessionClient)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var token = context.Request.Cookies["am_token"];
            if (!string.IsNullOrEmpty(token))
                context.Items["Company"] = await sessionClient.GetAsync(token);
        }

        await next(context);
    }
}
