using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
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

    public class SharedUploadResult
    {
        public string FileName { get; set; }
        public string UploadedBy { get; set; }
        public string SourceApp { get; set; }
        public DateTime UploadedAtUtc { get; set; }
        public long SizeBytes { get; set; }
    }

    public class FormSubmissionResult
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string SubmittedBy { get; set; }
        public string SourceApp { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
        public string FileName { get; set; }
        public long? FileSizeBytes { get; set; }
    }

    public class SessionResult
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
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

        // The global /uploads endpoint on Auth Manager - owned by neither app. The browser
        // posts the file to this controller (same origin), which then forwards it
        // server-to-server with the user's token, same contract the modern app uses.
        public SharedUploadResult UploadShared(string token, HttpPostedFileBase file)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using (var content = new MultipartFormDataContent())
                {
                    var fileContent = new StreamContent(file.InputStream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                        string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);
                    content.Add(fileContent, "file", file.FileName);
                    content.Add(new StringContent("legacy"), "source");

                    var response = client.PostAsync(BaseUrl + "/uploads", content).Result;
                    if (!response.IsSuccessStatusCode) return null;

                    var json = response.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<SharedUploadResult>(json);
                }
            }
        }

        public List<SharedUploadResult> ListShared(string token)
        {
            using (var client = new WebClient { Encoding = Encoding.UTF8 })
            {
                client.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
                try
                {
                    var json = client.DownloadString(BaseUrl + "/uploads");
                    return JsonConvert.DeserializeObject<List<SharedUploadResult>>(json);
                }
                catch (WebException)
                {
                    return new List<SharedUploadResult>();
                }
            }
        }

        // The global /forms endpoint - structured fields plus an optional file, same
        // contract and same history whether it was posted from here or from the modern app.
        public FormSubmissionResult SubmitSharedForm(
            string token, string firstName, string lastName, string email, HttpPostedFileBase file)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using (var content = new MultipartFormDataContent())
                {
                    content.Add(new StringContent(firstName ?? ""), "firstName");
                    content.Add(new StringContent(lastName ?? ""), "lastName");
                    content.Add(new StringContent(email ?? ""), "email");
                    content.Add(new StringContent("legacy"), "source");

                    if (file != null && file.ContentLength > 0)
                    {
                        var fileContent = new StreamContent(file.InputStream);
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                            string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);
                        content.Add(fileContent, "file", file.FileName);
                    }

                    var response = client.PostAsync(BaseUrl + "/forms", content).Result;
                    if (!response.IsSuccessStatusCode) return null;

                    var json = response.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<FormSubmissionResult>(json);
                }
            }
        }

        public List<FormSubmissionResult> ListSharedForms(string token)
        {
            using (var client = new WebClient { Encoding = Encoding.UTF8 })
            {
                client.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
                try
                {
                    var json = client.DownloadString(BaseUrl + "/forms");
                    return JsonConvert.DeserializeObject<List<FormSubmissionResult>>(json);
                }
                catch (WebException)
                {
                    return new List<FormSubmissionResult>();
                }
            }
        }
    }
}
