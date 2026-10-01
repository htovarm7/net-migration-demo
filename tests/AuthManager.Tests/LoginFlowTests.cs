using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuthManager.Api.Auth;
using AuthManager.Api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AuthManager.Tests;

public class LoginFlowTests : IClassFixture<LoginFlowTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        private readonly string _db = Path.Combine(Path.GetTempPath(), $"auth-test-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Default", $"Data Source={_db}");
            builder.UseSetting("Seed:Enabled", "true");
            builder.UseSetting("Jwt:Key", "test-key-with-more-than-32-characters-0123456789");
            builder.UseSetting("Routing:ModernBaseUrl", "http://modern");
            builder.UseSetting("Routing:LegacyBaseUrl", "http://legacy");
            builder.UseSetting("Routing:DefaultDestination", "Legacy");
            builder.UseSetting("Routing:Pages:0:Path", "/Home");
            builder.UseSetting("Routing:Pages:0:Destination", "Modern");
        }
    }

    private readonly Factory _factory;
    public LoginFlowTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task Login_WithLegacyHash_MigratesToV3_AndKeepsWorking()
    {
        var client = _factory.CreateClient();
        var req = new LoginRequest("user1", "12345678", Guid.NewGuid(), "/Home");

        var resp1 = await client.PostAsJsonAsync("/auth/login", req);
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);
        var r1 = (await resp1.Content.ReadFromJsonAsync<LoginResult>())!;
        Assert.True(r1.HashMigrated);
        Assert.Equal("Modern", r1.Destination);
        Assert.Equal("http://modern/Home", r1.RedirectUrl);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var hash = db.Users.Single(u => u.UserName == "user1").PasswordHash;
            Assert.Equal(PasswordHashFormat.IdentityV3, PasswordHashFormatDetector.Detect(hash));
        }

        var resp2 = await client.PostAsJsonAsync("/auth/login", req);
        var r2 = (await resp2.Content.ReadFromJsonAsync<LoginResult>())!;
        Assert.False(r2.HashMigrated);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var resp = await _factory.CreateClient()
            .PostAsJsonAsync("/auth/login", new LoginRequest("user2", "mala"));
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Route_RequiresToken_AndResolvesDestination()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/route?path=/Orders")).StatusCode);

        var login = await client.PostAsJsonAsync("/auth/login", new LoginRequest("user2", "AbcD$1234"));
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.GetAsync("/route?path=/Orders");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("legacy", await resp.Content.ReadAsStringAsync());
    }
}
