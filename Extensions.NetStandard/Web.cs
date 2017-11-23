using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Extensions.Net;
using System.Threading;

namespace Extensions
{
    public static class Web
    {
        public static byte[] GetFile(string uri)
        {
            var task = Task.Run(async () => await GetFile(new Uri(uri)));
            return task.Result;
        }

        static async Task<byte[]> GetFile(Uri uri)
        {
            using (var client = new WebClient())
            {
                return await client.DownloadDataTaskAsync(uri);
            }
        }

        public static async Task<dynamic> CallJsonApiAsync(HttpMethods httpMethod, string url, string username=null, string password=null, object body=null)
        {
            using (HttpClient client = new HttpClient())
            {
                if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(password))
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));
                }

                HttpContent httpContent=null;
                if (body != null)
                {
                    if (!httpMethod.IsAny(HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch))throw new ArgumentOutOfRangeException(nameof(httpMethod), "HttpMethod must be Post, Put or Patch when a body is specified");

                    var json = body is string ? body as string: JsonConvert.SerializeObject(body);
                    if (string.IsNullOrWhiteSpace(json)) throw new ArgumentNullException("body","json body is empty");
                    httpContent = new StringContent(json, Encoding.UTF8, httpMethod==HttpMethods.Patch ? "application/json-patch+json" : "application/json");

                }
                else if (httpMethod.IsAny(HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch))
                    throw new ArgumentOutOfRangeException(nameof(body), "You must supply a body when Post, Put or Patch when a body is specified");

                int retries = 0;
                retry:
                try
                {
                    using (var response = (
                        httpMethod == HttpMethods.Get ? client.GetAsync(url) :
                        httpMethod == HttpMethods.Delete ? client.DeleteAsync(url) :
                        httpMethod == HttpMethods.Post ? client.PostAsync(url, httpContent) :
                        httpMethod == HttpMethods.Put ? client.PutAsync(url, httpContent) :
                        httpMethod == HttpMethods.Patch ? client.PatchAsync(url, httpContent) :
                        throw new ArgumentOutOfRangeException(nameof(httpMethod), "HttpMethod must be Get, Delete, Post or Put")
                        ).Result)
                    {
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                        return jsonResponse;
                    }
                }
                catch (Exception ex)
                {
                    var rex = ex.InnerException?.InnerException ?? ex.InnerException ?? ex;
                    if (rex.Message.ContainsI("The remote server returned an error","503", "Server Unavailable"))
                    {
                        retries++;
                        if (retries < 10)
                        {
                            Thread.Sleep(1000);
                            goto retry;
                        }
                    }
                }
               
            }
            return null;
        }

    }
}