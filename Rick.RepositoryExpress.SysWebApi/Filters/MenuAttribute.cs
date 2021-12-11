using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.SysWebApi.Filters
{
    public class MenuAttribute : Attribute
    {
        public string[] Names { get; set; }
        public MenuAttribute(params string[] names)
        {
            Names = names;
        }
    }
}
