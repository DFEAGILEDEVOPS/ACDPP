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
    }
}