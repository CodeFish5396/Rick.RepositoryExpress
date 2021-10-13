using Rick.RepositoryExpress.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.SysWebApi.Models
{
    public class RickWebResult<T> where T : class
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public T Detail { get; set; }

    }
    public static class RickWebResult
    {
        public static RickWebResult<T> Success<T>(T t) where T : class
        {
            RickWebResult<T> rickWebResult = new RickWebResult<T>();
            rickWebResult.Message = ConstString.Success;
            rickWebResult.Detail = t;
            return rickWebResult;
        }
        public static RickWebResult<T> Error<T>(T t, int code, string message) where T : class
        {
            RickWebResult<T> rickWebResult = new RickWebResult<T>();
            rickWebResult.Message = message;
            rickWebResult.Code = code;
            rickWebResult.Detail = t;
            return rickWebResult;
        }
        public static RickWebResult<object> Unauthorized()
        {
             
            RickWebResult<object> rickWebResult = new RickWebResult<object>();
            rickWebResult.Message = ConstString.Unauthorized;
            rickWebResult.Code = 902;
            rickWebResult.Detail = new object();
            return rickWebResult;
        }

    }
}
