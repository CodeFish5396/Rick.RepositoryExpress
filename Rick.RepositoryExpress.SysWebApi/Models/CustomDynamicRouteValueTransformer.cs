using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.SysWebApi.Models
{
    public class CustomDynamicRouteValueTransformer : DynamicRouteValueTransformer
    {
        private string _route = string.Empty;
        public CustomDynamicRouteValueTransformer(string route)
        {
            _route = route;
        }
        public override async ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            if (!values.ContainsKey(_route))
            {
                return values;
            }
            else
            {
                var dir = (string)values[_route];
                values["Action"] = httpContext.Request.Method;
                //values.Remove(_route);
                return values;
            }
        }
    }
}
