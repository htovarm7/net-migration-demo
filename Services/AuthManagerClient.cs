using System;
using System.Configuration;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LegacyWeb.Services
{
    public class LoginResult
    {
        public string Token { get; set; }
        public DateTime ExpiresUtc { get; set; }
        public bool HashMigrated { get; set; }
        public string Destination { get; set; }
        public string RedirectUrl { get; set; }
    }

    public class RouteResult
    {
        public string Destination { get; set; }
        public string Url { get; set; }
    }

    public class SessionResult
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
    }

    public class AuthManagerClient
    {
        private static string BaseUrl =>
            ConfigurationManager.AppSettings["AuthManagerBaseUrl"].TrimEnd('/');

        public LoginResult Login(string user, string password, string returnPath)
        {
            var body = JsonConvert.SerializeObject(new { user, password, returnPath });

            using (var client = new WebClient { Encoding = Encoding.UTF8 })
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                try
                {
                    var json = client.UploadString(BaseUrl + "/auth/login", "POST", body);
                    return JsonConvert.DeserializeObject<LoginResult>(json);
                }
                catch (WebException)
                {
                    return null;
                }
            }
        }

        public RouteResult Route(string path, string token)
        {
            using (var client = new WebClient { Encoding = Encoding.UTF8 })
            {
                client.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
                try
                {
                    var json = client.DownloadString(BaseUrl + "/route?path=" + Uri.EscapeDataString(path));
                    var obj = JObject.Parse(json);
                    return new RouteResult
                    {
                        Destination = (string)obj["destination"],
                        Url = (string)obj["url"]
                    };
                }
                catch (WebException)
                {
                    return null;
                }
            }
        }

        // Session/company state, backed by Redis on Auth Manager's side. Both this app and the
        // modern one read the same key for a user through this endpoint, so they never disagree.
        public SessionResult Session(string token)
        {
            using (var client = new WebClient { Encoding = Encoding.UTF8 })
            {
                client.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
                try
                {
                    var json = client.DownloadString(BaseUrl + "/session");
                    return JsonConvert.DeserializeObject<SessionResult>(json);
                }
                catch (WebException)
                {
                    return null;
                }
            }
        }
    }
}
