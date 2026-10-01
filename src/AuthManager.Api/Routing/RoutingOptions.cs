namespace AuthManager.Api.Routing;

public enum Destination
{
    Legacy,
    Modern
}

public class PageRoute
{
    public string Path { get; set; } = "/";
    public Destination Destination { get; set; }
}

public class RoutingOptions
{
    public string LegacyBaseUrl { get; set; } = "";
    public string ModernBaseUrl { get; set; } = "";
    public Destination DefaultDestination { get; set; } = Destination.Legacy;
    public List<PageRoute> Pages { get; set; } = [];
}
