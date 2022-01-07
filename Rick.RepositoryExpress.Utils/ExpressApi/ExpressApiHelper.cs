using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.Utils.ExpressApi
{
    public class ExpressApiHelper
    {
        private const String host = "https://wdexpress.market.alicloudapi.com";
        private const String path = "/gxali";
        private const String method = "GET";
        private const String appcode = "72bd9505062e41c6bff91c7b3617636c";

        public static async Task<string> Get(string expressNumber, string typeCode = null)
        {
            HttpClient httpClient = new HttpClient();
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.RequestUri = new Uri(host + path + "?n=" + expressNumber + (string.IsNullOrEmpty(typeCode) ? "" : ("&t=" + typeCode)));
            httpRequestMessage.Method = new HttpMethod(method);
            httpRequestMessage.Headers.Add("Authorization", "APPCODE " + appcode);

            HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
            byte[] resultByte = await httpResponseMessage.Content.ReadAsByteArrayAsync();
            string result = Encoding.UTF8.GetString(resultByte);
            return result;
        }
    }
}
