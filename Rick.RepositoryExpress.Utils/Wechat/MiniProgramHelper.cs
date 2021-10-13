using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Rick.RepositoryExpress.Utils.Wechat
{
    public class MiniProgramHelper
    {
        public static async Task<MiniProgramLoginResult> GetOpenidAndSessionkey(string code)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage httpResponseMessage = await httpClient.GetAsync("https://api.weixin.qq.com/sns/jscode2session?appid=wxe715ee2398e8d4c6&secret=50d16a5a35f7e9ac01c2c21589967629&js_code=" + code + "&grant_type=authorization_code");
            string result = await httpResponseMessage.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<MiniProgramLoginResult>(result);
        }

        public static MiniProgramUserResult Descrypt(string encrypedData, string sessionKey, string iv,out string message)
        {
            try
            {
                var decryptBytes = Convert.FromBase64String(encrypedData);
                var keyBytes = Convert.FromBase64String(sessionKey);
                var ivBytes = Convert.FromBase64String(iv);
                var outputBytes = DescryptByAesBytes(decryptBytes, keyBytes, ivBytes);
                string result = Encoding.UTF8.GetString(outputBytes);
                message = string.Empty;
                return JsonConvert.DeserializeObject<MiniProgramUserResult>(result);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return null;
            }
        }
        public static byte[] DescryptByAesBytes(byte[] decryptedBytes, byte[] keyBytes, byte[] ivBytes,CipherMode cipher = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
        {
            if (decryptedBytes == null || decryptedBytes.Length <= 0)
                throw new ArgumentNullException("DecryptedBytes");
            if (decryptedBytes == null || decryptedBytes.Length <= 0)
                throw new ArgumentNullException("KeyBytes");
            if (decryptedBytes == null || decryptedBytes.Length <= 0)
                throw new ArgumentNullException("IvBytes");
            var aes = new AesCryptoServiceProvider
            {
                Key = keyBytes,
                IV = ivBytes,
                Mode = cipher,
                Padding = padding
            };
            var outputBytes = aes.CreateDecryptor().TransformFinalBlock(decryptedBytes, 0, decryptedBytes.Length);
            return outputBytes;
        }

    }
    public class MiniProgramLoginResult
    {
        public string OpenId { get; set; }
        public string Session_Key { get; set; }
    }

    public class MiniProgramUserResult
    {
        public string PhoneNumber { get; set; }
        public string PurePhoneNumber { get; set; }
        public string Countrycode { get; set; }

    }

}
