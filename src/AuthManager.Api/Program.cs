using System.Security.Claims;
using System.Text;
using AuthManager.Api.Auth;
using AuthManager.Api.Data;
using AuthManager.Api.Routing;
using AuthManager.Api.Session;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

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

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379"));
builder.Services.AddSingleton<SessionStateStore>();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters (set it via the Jwt__Key environment variable).");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
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

// Session/company state, backed by Redis. Both the legacy and the modern app read this
// same key for a user, so neither one can drift from the other.
app.MapGet("/session", async (ClaimsPrincipal user, SessionStateStore store) =>
{
    var userId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (userId is null) return Results.Unauthorized();

    var session = await store.GetAsync(userId);
    return session is null ? Results.NotFound() : Results.Ok(session);
}).RequireAuthorization();

app.Run();

public partial class Program;
