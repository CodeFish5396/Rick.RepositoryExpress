using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Rick.RepositoryExpress.WebApi.Models
{
    public class AddAuthTokenHeaderParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                if (descriptor.ControllerTypeInfo.BaseType == typeof(RickControllerBase))
                {
                    var attributes = descriptor.MethodInfo.GetCustomAttributes(true);
                    if (attributes.Where(t => typeof(AllowAnonymousAttribute) == t.GetType()).Count() > 0)
                    {
                        return;
                    }
                    else
                    {
                        operation.Parameters.Add(new OpenApiParameter()
                        {
                            Name = "AuthToken",
                            In = ParameterLocation.Header,//query header body path formData
                            Required = true //是否必选
                        });
                    }
                }
            }
        }
    }
}
