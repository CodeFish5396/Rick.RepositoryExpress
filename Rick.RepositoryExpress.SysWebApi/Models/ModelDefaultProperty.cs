using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

namespace Rick.RepositoryExpress.SysWebApi.Models
{
    public static class ModelDefaultProperty
    {
        public static void Init<T>(T t) where T : class
        {
            Type type = t.GetType();
            var properties = type.GetProperties();
            foreach (var property in properties)
            {
            }
        }
    }
}
