using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rick.RepositoryExpress.SysWebApi.Models;
using Rick.RepositoryExpress.IService;
using Rick.RepositoryExpress.RedisService;
using Rick.RepositoryExpress.Common;
using Rick.RepositoryExpress.DataBase.Models;
using Newtonsoft.Json;

namespace Rick.RepositoryExpress.SysWebApi.Filters
{
    public class CustomAuthorizationFilterAttribute : Attribute, IAuthorizationFilter
    {
        private readonly ILogger<CustomAuthorizationFilterAttribute> _logger;
        private readonly ISysuserService _sysuserService;
        private readonly RedisClientService _redisClientService;
        public CustomAuthorizationFilterAttribute(ILogger<CustomAuthorizationFilterAttribute> logger, ISysuserService sysuserService, RedisClientService redisClientService)
        {
            _logger = logger;
            _sysuserService = sysuserService;
            _redisClientService = redisClientService;
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
                            //解析Token获得UserId等
                            UserInfo userInfo = AuthTokenHelper.Get(token);
                            if (attributes.Where(t => typeof(AdminAttribute) == t.GetType()).Count() > 0)
                            {
                                if (userInfo.Name != "root")
                                {
                                    context.Result = new OkObjectResult(RickWebResult.Unauthorized());
                                }
                            }
                            else
                            {
                                string cachedToken = _redisClientService.HashGet(ConstString.RickUserLoginKey, userInfo.Id.ToString());
                                if (string.IsNullOrEmpty(cachedToken) || cachedToken != token)
                                {
                                    context.Result = new OkObjectResult(RickWebResult.Unauthorized());
                                }
                                else if (!userInfo.IsDefaultRole)
                                {
                                    string functionTypeName = string.Empty;
                                    string[] menuNames = null;

                                    //MenuAttribute 菜单index
                                    MenuAttribute menuAttribute = attributes.FirstOrDefault(t => typeof(MenuAttribute) == t.GetType()) as MenuAttribute;
                                    if (menuAttribute != null)
                                    {
                                        menuNames = menuAttribute.Names;
                                    }
                                    else
                                    {
                                        menuAttribute = descriptor.ControllerTypeInfo.GetCustomAttributes(true).FirstOrDefault(t => typeof(MenuAttribute) == t.GetType()) as MenuAttribute;
                                        if (menuAttribute != null)
                                        {
                                            menuNames = menuAttribute.Names;
                                        }
                                    }

                                    //FunctionAttribute Function名称
                                    FunctionAttribute functionAttribute = attributes.FirstOrDefault(t => typeof(FunctionAttribute) == t.GetType()) as FunctionAttribute;
                                    if (functionAttribute != null)
                                    {
                                        functionTypeName = functionAttribute.TypeName;
                                    }
                                    else
                                    {
                                        string actionName = descriptor.ActionName;
                                        switch (actionName)
                                        {
                                            case "Get":
                                                functionTypeName = "Retrieve";
                                                break;
                                            case "Post":
                                                functionTypeName = "Create";
                                                break;
                                            case "Put":
                                                functionTypeName = "Update";
                                                break;
                                            case "Patch":
                                                functionTypeName = "Update";
                                                break;
                                            case "Delete":
                                                functionTypeName = "Delete";
                                                break;
                                            default:
                                                functionTypeName = string.Empty;
                                                break;
                                        }
                                    }

                                    if (menuNames != null && menuNames.Length > 0 && !string.IsNullOrEmpty(functionTypeName))
                                    {
                                        string cachedRoleMenuFunctionInfos = _redisClientService.HashGet(ConstString.RickRoleMenuFunctionInfosKey, userInfo.Id.ToString());
                                        List<RoleMenuFunctionInfo> RoleMenuFunctionInfos = JsonConvert.DeserializeObject<List<RoleMenuFunctionInfo>>(cachedRoleMenuFunctionInfos);
                                        if (!RoleMenuFunctionInfos.Any(t => menuNames.Contains(t.Menuname) && t.FunctionTypeName == functionTypeName))
                                        {
                                            context.Result = new OkObjectResult(RickWebResult.Noauthorized());
                                        }
                                    }
                                }
                            }
                            context.HttpContext.Items.Add("TokenUserInfo", userInfo);
                        }
                    }
                }
            }
        }

    }
}
