using ModernWeb.Services;

namespace ModernWeb.Middleware;

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
