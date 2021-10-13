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

                userLoginResult.Token = AuthTokenHelper.Create(userInfo);
                return RickWebResult.Success(userLoginResult);
            }
        }
    }

    public class UserLoginRequest
    {
        public string Name { get; set; }
        public string PassWord { get; set; }
    }

    public class UserLoginResult
    {
        public string Token { get; set; }
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
