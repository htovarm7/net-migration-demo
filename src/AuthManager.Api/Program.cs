using System.Security.Claims;
using System.Text;
using AuthManager.Api.Auth;
using AuthManager.Api.Data;
using AuthManager.Api.Routing;
using AuthManager.Api.Session;
using AuthManager.Api.Forms;
using AuthManager.Api.Uploads;
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
builder.Services.AddSingleton<SharedUploadStore>();
builder.Services.AddSingleton<SharedFormStore>();

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

// A genuinely global endpoint: owned by neither legacy nor modern. Both call the exact
// same contract and see the exact same files, whichever one made the request.
app.MapPost("/uploads", async (HttpRequest req, SharedUploadStore uploads, ClaimsPrincipal user) =>
{
    var file = req.Form.Files["file"];
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { error = "file is required" });

    var uploadedBy = user.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? "unknown";
    var sourceApp = req.Form["source"].FirstOrDefault() ?? "unknown";

    var record = await uploads.SaveAsync(file, uploadedBy, sourceApp);
    return Results.Ok(record);
}).RequireAuthorization().DisableAntiforgery();

app.MapGet("/uploads", (SharedUploadStore uploads) => Results.Ok(uploads.List()))
    .RequireAuthorization();

// Same idea as /uploads, but for structured data plus history: both apps post the exact
// same fields and can read everyone's past submissions back, from a browser or a terminal.
app.MapPost("/forms", async (HttpRequest req, SharedFormStore forms, ClaimsPrincipal user) =>
{
    var firstName = req.Form["firstName"].FirstOrDefault() ?? "";
    var lastName = req.Form["lastName"].FirstOrDefault() ?? "";
    var email = req.Form["email"].FirstOrDefault() ?? "";
    var sourceApp = req.Form["source"].FirstOrDefault() ?? "unknown";
    var file = req.Form.Files["file"];

    var submittedBy = user.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ?? "unknown";
    var record = await forms.SaveAsync(firstName, lastName, email, submittedBy, sourceApp, file);
    return Results.Ok(record);
}).RequireAuthorization().DisableAntiforgery();

app.MapGet("/forms", (SharedFormStore forms) => Results.Ok(forms.List()))
    .RequireAuthorization();

app.Run();

public partial class Program;
