using Newtonsoft.Json;
using Rick.RepositoryExpress.Common;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.WebApi.Models
{
    public static class AuthTokenHelper
    {
        public static string Create(UserInfo userInfo)
        {
            string guid = Guid.NewGuid().ToString("N");
            string payload = JsonConvert.SerializeObject(userInfo);
            payload = AesClass.AesEncrypt(payload, ConstString.RickAesKey);
            string ticks = DateTime.Now.Ticks.ToString();
            return $"{guid}.{payload}.{ticks}";
        }

        public static UserInfo Get(string token)
        {
            string[] payloads = token.Split(".");
            string payload = payloads[1];
            payload = AesClass.AesDecrypt(payload, ConstString.RickAesKey);
            UserInfo userInfo = JsonConvert.DeserializeObject<UserInfo>(payload);
            return userInfo;
        }

    }
    public class UserInfo
    {
        public long Id { get; set; }
        public string Openid { get; set; }
        public string Mobile { get; set; }
        public string Countrycode { get; set; }
        public long Companyid { get; set; }
        public string Companyname { get; set; }

    }
}
