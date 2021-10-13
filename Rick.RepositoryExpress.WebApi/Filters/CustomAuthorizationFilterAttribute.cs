using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rick.RepositoryExpress.WebApi.Models;
using Rick.RepositoryExpress.IService;

namespace Rick.RepositoryExpress.WebApi.Filters
{
    public class CustomAuthorizationFilterAttribute : Attribute, IAuthorizationFilter
    {
        private readonly ILogger<CustomAuthorizationFilterAttribute> _logger;
        private readonly IAppuserService _appuserService;

        public CustomAuthorizationFilterAttribute(ILogger<CustomAuthorizationFilterAttribute> logger, IAppuserService appuserService)
        {
            _logger = logger;
            _appuserService = appuserService;

        }
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
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
                        //获取Token
                        string token = context.HttpContext.Request.Headers["AuthToken"];
                        if (string.IsNullOrEmpty(token))
                        {
                            context.Result = new OkObjectResult(RickWebResult.Unauthorized());
                        }
                        else
                        {
                            _logger.LogInformation(token);
                            //解析Token获得UserId等
                            UserInfo userInfo = AuthTokenHelper.Get(token);
                            if (userInfo == null || userInfo.Id <= 0 || userInfo.Companyid <= 0)
                            {
                                context.Result = new OkObjectResult(RickWebResult.Unauthorized());
                            }
                            else
                            {
                                context.HttpContext.Items.Add("TokenUserInfo", userInfo);
                            }
                        }
                    }
                }
            }
        }

    }
}
