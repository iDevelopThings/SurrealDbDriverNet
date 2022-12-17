using IocContainer;

namespace Driver;

public interface IDatabaseConfiguration
{
    /// <summary>
    /// Should be something like: "http://127.0.0.1:8082"
    /// </summary>
    string? Address { get; set; }

    /// <summary>
    /// The name of your database
    /// </summary>
    string? DatabaseName { get; set; }

    /// <summary>
    /// The namespace for your database
    /// </summary>
    string? Namespace { get; set; }

    string? AuthUsername { get; set; }
    string? AuthPassword { get; set; }
    Uri     Uri          { get; }
    bool    IsSecure     { get; }
    string  WsProtocol   { get; }
    string  HttpProtocol { get; }
    Uri     RpcUri       { get; }
}

public class DatabaseConfiguration : IDatabaseConfiguration
{
    /// <summary>
    /// Should be something like: "http://127.0.0.1:8082"
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// The name of your database
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The namespace for your database
    /// </summary>
    public string? Namespace { get; set; }

    public string? AuthUsername { get; set; }

    public string? AuthPassword { get; set; }

    public Uri Uri => new(Address!);

    public bool   IsSecure     => Uri.Scheme == "https";
    public string WsProtocol   => IsSecure ? "wss" : "ws";
    public string HttpProtocol => IsSecure ? "https" : "http";

    public Uri RpcUri
    {
        get
        {
            var uriBuilder = new UriBuilder(Uri) {
                Scheme = WsProtocol,
                Path   = Uri.AbsolutePath != "/rpc" ? "/rpc" : Uri.AbsolutePath,
                Port   = Uri.Port == -1 ? (IsSecure ? 443 : 80) : Uri.Port
            };
            return uriBuilder.Uri;
        }
    }

}