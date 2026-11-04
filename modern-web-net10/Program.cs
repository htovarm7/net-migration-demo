using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModernWeb.Middleware;
using ModernWeb.Services;

var builder = WebApplication.CreateBuilder(args);

var jwt = builder.Configuration.GetSection("Jwt");
var legacyLoginUrl = builder.Configuration["Legacy:LoginUrl"]!;
var authManagerBaseUrl = builder.Configuration["AuthManager:BaseUrl"]!;

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<SessionClient>(c => c.BaseAddress = new Uri(authManagerBaseUrl));
builder.Services.AddHttpClient<SharedFilesClient>(c => c.BaseAddress = new Uri(authManagerBaseUrl));
builder.Services.AddHttpClient<SharedFormsClient>(c => c.BaseAddress = new Uri(authManagerBaseUrl));

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
            // The token lives in the same cookie the legacy app sets - the gateway in front
            // of both apps forwards under one host (miicel.local) with no visible prefix.
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
app.UseMiddleware<CompanyContextMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .RequireAuthorization();

app.Run();
