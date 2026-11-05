using Microsoft.Extensions.Options;

namespace AuthManager.Api.Routing;

public record ResolvedRoute(Destination Destination, string Url);

public class RouteResolver(IOptions<RoutingOptions> options)
{
    private readonly RoutingOptions _opts = options.Value;

    public ResolvedRoute Resolve(string path)
    {
        path = Normalize(path);

        var destination = _opts.Pages
            .Where(p => MatchesPrefix(path, Normalize(p.Path)))
            .OrderByDescending(p => Normalize(p.Path).Length)
            .Select(p => (Destination?)p.Destination)
            .FirstOrDefault() ?? _opts.DefaultDestination;

        var baseUrl = (destination == Destination.Modern ? _opts.ModernBaseUrl : _opts.LegacyBaseUrl).TrimEnd('/');
        return new ResolvedRoute(destination, baseUrl + path);
    }

    private static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        return path.StartsWith('/') ? path : "/" + path;
    }

    private static bool MatchesPrefix(string path, string prefix)
    {
        if (prefix == "/") return true;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        return path.Length == prefix.Length || path[prefix.Length] is '/' or '?';
    }
}
