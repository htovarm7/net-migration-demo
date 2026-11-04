using System.Net;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

var legacyUpstream = builder.Configuration["Upstreams:Legacy"]!;
var modernUpstream = builder.Configuration["Upstreams:Modern"]!;
var authManagerBaseUrl = builder.Configuration["AuthManager:BaseUrl"]!;

builder.Services.AddHttpForwarder();
builder.Services.AddHttpClient("auth-manager", c => c.BaseAddress = new Uri(authManagerBaseUrl));

var app = builder.Build();

var httpClient = new HttpMessageInvoker(new SocketsHttpHandler
{
    UseProxy = false,
    AllowAutoRedirect = false,
    AutomaticDecompression = DecompressionMethods.None,
    UseCookies = false
});

// Same request, every time: decide once, server-side, which app answers it - the
// browser's address bar never changes and never reveals which one responded.
app.Map("/{**catchAll}", async (HttpContext context, IHttpForwarder forwarder, IHttpClientFactory httpClientFactory) =>
{
    var path = context.Request.Path.Value ?? "/";
    var destination = await ResolveDestinationAsync(path, context.Request.Cookies["am_token"], httpClientFactory);

    var transformer = destination == legacyUpstream
        ? HostOverrideTransformer.ToLocalhost
        : HttpTransformer.Default;

    var error = await forwarder.SendAsync(context, destination, httpClient, ForwarderRequestConfig.Empty, transformer);
    if (error != ForwarderError.None)
    {
        var errorFeature = context.GetForwarderErrorFeature();
        app.Logger.LogError(errorFeature?.Exception, "Forwarding to {Destination} failed: {Error}", destination, error);
    }
});

app.Run();

async Task<string> ResolveDestinationAsync(string path, string? token, IHttpClientFactory httpClientFactory)
{
    // Login/logout only ever live on the legacy app - never ask Auth Manager about them.
    if (path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase))
        return legacyUpstream;

    // No session yet: send to legacy, whose own filter will bounce to /Account/Login.
    if (string.IsNullOrEmpty(token))
        return legacyUpstream;

    try
    {
        using var client = httpClientFactory.CreateClient("auth-manager");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/route?path={Uri.EscapeDataString(path)}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return legacyUpstream;

        var json = await response.Content.ReadFromJsonAsync<RouteResponse>();
        return json?.Destination == "Modern" ? modernUpstream : legacyUpstream;
    }
    catch
    {
        // Auth Manager unreachable: fail toward legacy rather than dropping the request.
        return legacyUpstream;
    }
}

record RouteResponse(string Destination, string Url);

// IIS Express only accepts requests whose Host header is exactly "localhost" - this
// overrides it on the way out, same trick Traefik used before the gateway existed.
class HostOverrideTransformer : HttpTransformer
{
    public static readonly HostOverrideTransformer ToLocalhost = new();

    public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix, CancellationToken cancellationToken)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);
        proxyRequest.Headers.Host = "localhost";
    }
}
