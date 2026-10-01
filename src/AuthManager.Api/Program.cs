using System.Text;
using AuthManager.Api.Auth;
using AuthManager.Api.Data;
using AuthManager.Api.Routing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<RoutingOptions>(builder.Configuration.GetSection("Routing"));

builder.Services.AddDbContext<AuthDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Identity Core's PasswordHasher<T> verifies v2 hashes (.NET Framework) and returns SuccessRehashNeeded.
builder.Services
    .AddIdentityCore<AppUser>(o =>
    {
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AuthDbContext>();

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddSingleton<RouteResolver>();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters (set it via the Jwt__Key environment variable).");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ClockSkew = TimeSpan.FromSeconds(30)
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Seed:Enabled"))
{
    using var scope = app.Services.CreateScope();
    await SeedData.InitializeAsync(scope.ServiceProvider.GetRequiredService<AuthDbContext>());
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/auth/login", async (LoginRequest req, LoginService login) =>
{
    if (string.IsNullOrWhiteSpace(req.User) || string.IsNullOrEmpty(req.Password))
        return Results.BadRequest(new { error = "user and password are required" });

    var result = await login.LoginAsync(req);
    return result is null
        ? Results.Json(new { error = "invalid credentials" }, statusCode: StatusCodes.Status401Unauthorized)
        : Results.Ok(result);
});

// Tells which app (legacy or new) owns a page.
app.MapGet("/route", (string path, RouteResolver router) => Results.Ok(router.Resolve(path)))
    .RequireAuthorization();

app.Run();

public partial class Program;
