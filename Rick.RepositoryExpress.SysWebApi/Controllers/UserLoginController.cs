using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rick.RepositoryExpress.SysWebApi.Models;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using Rick.RepositoryExpress.Common;
using Microsoft.AspNetCore.Authorization;
using Rick.RepositoryExpress.Utils.Wechat;
using Rick.RepositoryExpress.RedisService;
using Rick.RepositoryExpress.Utils;
using Newtonsoft.Json;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserLoginController : RickControllerBase
    {
        private readonly ILogger<UserLoginController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ISysuserService _sysuserService;
        private readonly RedisClientService _redisClientService;

        public UserLoginController(ILogger<UserLoginController> logger, ISysuserService sysuserService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _sysuserService = sysuserService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<RickWebResult<UserLoginResult>> Get([FromQuery] string username, [FromQuery] string password, [FromQuery] long companyId = 15464799164941312)
        {
            UserLoginResult userLoginResult = new UserLoginResult();
            Sysuser sysuser = await _sysuserService.FindAsync<Sysuser>(t => t.Name == username && t.Password == password.ToUpper() && t.Status == 1);
            if (sysuser == null)
            {
                return RickWebResult.Error(userLoginResult, 996, "用户名或密码错误");
            }
            else
            {
                UserInfo userInfo = new UserInfo();
                userInfo.Id = sysuser.Id;
                userInfo.Name = sysuser.Name;
                userInfo.Companyid = companyId;
                Syscompany syscompany = await _sysuserService.FindAsync<Syscompany>(userInfo.Companyid);
                if (syscompany == null || syscompany.Status != 1)
                {
                    return RickWebResult.Error(new UserLoginResult(), 997, "授权已过期");
                }
                userInfo.Companyname = syscompany.Name;

                var userRoles = await _sysuserService.QueryAsync<Sysuserrole>(t => t.Status == 1 && t.Companyid == userInfo.Companyid && t.Userid == userInfo.Id);
                userLoginResult.Roles = userRoles.Select(t => t.Roleid).ToList();
                List<RoleMenuFunctionInfo> RoleMenuFunctionInfos = new List<RoleMenuFunctionInfo>();
                var roles = await _sysuserService.QueryAsync<Sysrole>(t => userLoginResult.Roles.Contains(t.Id) && t.Status == 1);
                userInfo.IsDefaultRole = roles.Any(t => t.Isdefault == 1);
                if (!userInfo.IsDefaultRole)
                {
                    var sysrolemenufunctions = await _sysuserService.QueryAsync<Sysrolemenufunction>(t => t.Status == 1 && t.Companyid == userInfo.Companyid && userLoginResult.Roles.Contains(t.Roleid));

                    var menuIds = sysrolemenufunctions.Select(t => t.Menuid);
                    var functionIds = sysrolemenufunctions.Select(t => t.Functionid);

                    var menus = await _sysuserService.QueryAsync<Sysmenu>(t => menuIds.Contains(t.Id));
                    var functions = await _sysuserService.QueryAsync<Sysfunction>(t => functionIds.Contains(t.Id));

                    RoleMenuFunctionInfos = (from sysrolemenufunction in sysrolemenufunctions
                                                      join menu in menus
                                                      on sysrolemenufunction.Menuid equals menu.Id
                                                      join function in functions
                                                      on sysrolemenufunction.Functionid equals function.Id
                                                      select new RoleMenuFunctionInfo()
                                                      {
                                                          Menuname = menu.Name,
                                                          Menuindex = menu.Index,
                                                          FunctionTypeName = function.Typename
                                                      }).Distinct().ToList();

                    var baseMenuIndexs = RoleMenuFunctionInfos.Select(t => t.Menuindex).Distinct().ToList();

                    userLoginResult.MenuIndexs = new List<string>();
                    userLoginResult.MenuIndexs.AddRange(baseMenuIndexs);
                    string sysmenuTreeResponsesString = _redisClientService.StringGet(ConstString.RickMenuTreeKey);

                    var menuTree = JsonConvert.DeserializeObject<List<SysmenuTreeResponse>>(sysmenuTreeResponsesString);

                    foreach (var fisrtLayerMenu in menuTree)
                    {
                        bool firstISSee = false;
                        if (fisrtLayerMenu.Isdirectory == 1)
                        {                            
                            foreach (var secondLayerMenu in fisrtLayerMenu.Subs)
                            {
                                if (secondLayerMenu.Isdirectory == 0 && baseMenuIndexs.Contains(secondLayerMenu.Index))
                                {
                                    firstISSee = true;
                                }
                                bool secondISSee = false;

                                if (secondLayerMenu.Isdirectory == 1)
                                {
                                    foreach (var thirdLayerMenu in secondLayerMenu.Subs)
                                    {
                                        if (thirdLayerMenu.Isdirectory == 0 && baseMenuIndexs.Contains(thirdLayerMenu.Index))
                                        {
                                            secondISSee = true;
                                        }
                                    }
                                    if (secondISSee)
                                    {
                                        userLoginResult.MenuIndexs.Add(fisrtLayerMenu.Index);
                                        userLoginResult.MenuIndexs.Add(secondLayerMenu.Index);
                                    }
                                }
                            }
                            if (firstISSee)
                            {
                                userLoginResult.MenuIndexs.Add(fisrtLayerMenu.Index);
                            }
                        }
                    }

                }
                userInfo.RoleMenuFunctionInfos = Guid.NewGuid().ToString("N");
                _redisClientService.HashSet(ConstString.RickRoleMenuFunctionInfosKey, userInfo.Id.ToString(), JsonConvert.SerializeObject(RoleMenuFunctionInfos));
                userLoginResult.Token = AuthTokenHelper.Create(userInfo);
                userLoginResult.IsDefaultRole = userInfo.IsDefaultRole;
                userLoginResult.RoleMenuFunctionInfos = RoleMenuFunctionInfos;
                _redisClientService.HashSet(ConstString.RickUserLoginKey, userInfo.Id.ToString(), userLoginResult.Token);
                return RickWebResult.Success(userLoginResult);
            }
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="userLoginPutRequest"></param>
        /// <returns></returns>
        [HttpPut]
        [AllowAnonymous]
        public async Task<RickWebResult<object>> Put([FromBody] UserLoginPutRequest userLoginPutRequest)
        {
            Sysuser sysuser = await _sysuserService.FindAsync<Sysuser>(t => t.Name == userLoginPutRequest.Username && t.Password == userLoginPutRequest.Oldpassword.ToUpper() && t.Status == 1);
            if (sysuser == null)
            {
                return RickWebResult.Error(new object(), 996, "用户名或密码错误");
            }
            else
            {
                sysuser.Password = userLoginPutRequest.Newpassword.ToUpper();
                sysuser.Lasttime = DateTime.Now;
                await _sysuserService.UpdateAsync(sysuser);
                return RickWebResult.Success(new object());
            }
        }
    }

    public class UserLoginRequest
    {
        public string Name { get; set; }
        public string PassWord { get; set; }
    }

    public class UserLoginPutRequest
    {
        public string Username { get; set; }
        public string Oldpassword { get; set; }
        public string Newpassword { get; set; }
    }


    public class UserLoginResult
    {
        public string Token { get; set; }
        public bool IsDefaultRole { get; set; }
        public List<long> Roles { get; set; }
        public List<string> MenuIndexs { get; set; }
        public List<RoleMenuFunctionInfo> RoleMenuFunctionInfos { get; set; }

    }

    public class CommonResult
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }
    public class CommonSimpleRequest
    {
        public string Name { get; set; }
    }
}
