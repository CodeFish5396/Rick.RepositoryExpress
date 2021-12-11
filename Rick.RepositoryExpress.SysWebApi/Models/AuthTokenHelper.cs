using Newtonsoft.Json;
using Rick.RepositoryExpress.Common;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.SysWebApi.Models
{
    public static class AuthTokenHelper
    {
        public static string Create(UserInfo userInfo)
        {
            string guid = Guid.NewGuid().ToString("N");
            string payload = JsonConvert.SerializeObject(userInfo);
            payload = AesClass.AesEncrypt(payload, ConstString.RickAesKey);
            string ticks = DateTime.Now.Ticks.ToString();
            string rawResult = $"{guid}.{payload}.{ticks}";
            string result = GZipString.GZipCompressString(rawResult);
            return result;
        }

        public static UserInfo Get(string token)
        {
            string rawToken = GZipString.GetStringByString(token);
            string[] payloads = rawToken.Split(".");
            string payload = payloads[1];
            payload = AesClass.AesDecrypt(payload, ConstString.RickAesKey);
            UserInfo userInfo = JsonConvert.DeserializeObject<UserInfo>(payload);
            return userInfo;
        }

    }

    public class UserInfo
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long Companyid { get; set; }
        public string Companyname { get; set; }
        public bool IsDefaultRole { get; set; }
        public string RoleMenuFunctionInfos { get; set; }
        //public List<RoleMenuFunctionInfo> RoleMenuFunctionInfos { get; set; }

    }

    public class RoleMenuFunctionInfo
    {
        public string Menuname { get; set; }
        public string Menuindex { get; set; }
        public string FunctionTypeName { get; set; }

    }
}
