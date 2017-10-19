using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;

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

        public static async Task<dynamic> CallJsonApiAsync(HttpMethod httpMethod, string url, string username=null, string password=null, object body=null)
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
                    if (!httpMethod.IsAny(HttpMethod.Post, HttpMethod.Put))throw new ArgumentOutOfRangeException(nameof(httpMethod), "HttpMethod must be Post or Put when a body is specified");

                    var json = JsonConvert.SerializeObject(body);
                    httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                }
                else if (httpMethod.IsAny(HttpMethod.Post, HttpMethod.Put))
                    throw new ArgumentOutOfRangeException(nameof(body), "You must supply a body when Post or Put when a body is specified");


                using (var response = (
                    httpMethod == HttpMethod.Get ? client.GetAsync(url) :
                    httpMethod == HttpMethod.Delete ? client.DeleteAsync(url) :
                    httpMethod == HttpMethod.Post ? client.PostAsync(url, httpContent) :
                    httpMethod == HttpMethod.Put ? client.PutAsync(url, httpContent):
                    throw new ArgumentOutOfRangeException(nameof(httpMethod),"HttpMethod must be Get, Delete, Post or Put")
                    ).Result)
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                    return jsonResponse;
                }
            }
        }

    }
}