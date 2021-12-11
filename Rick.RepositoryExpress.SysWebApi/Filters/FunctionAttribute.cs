using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.SysWebApi.Filters
{
    public class FunctionAttribute : Attribute
    {
        public string TypeName { get; set; }
        public FunctionAttribute(string typeName)
        {
            TypeName = typeName;
        }
    }
}
