using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwt = builder.Configuration.GetSection("Jwt");
var legacyLoginUrl = builder.Configuration["Legacy:LoginUrl"]!;

builder.Services.AddControllersWithViews();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            NameClaimType = "unique_name",
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        o.Events = new JwtBearerEvents
        {
            // The token lives in the same cookie the legacy app sets (cookies are shared across localhost ports).
            OnMessageReceived = ctx =>
            {
                ctx.Token = ctx.Request.Cookies["am_token"];
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                var returnPath = ctx.Request.Path + ctx.Request.QueryString;
                ctx.Response.Redirect($"{legacyLoginUrl}?returnPath={Uri.EscapeDataString(returnPath)}");
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .RequireAuthorization();

app.Run();
