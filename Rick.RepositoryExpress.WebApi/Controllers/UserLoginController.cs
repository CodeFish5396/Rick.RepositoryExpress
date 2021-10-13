using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rick.RepositoryExpress.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Rick.RepositoryExpress.IService;
using Rick.RepositoryExpress.Common;
using Rick.RepositoryExpress.DataBase.Models;

namespace Rick.RepositoryExpress.WebApi.Controllers
{
    [Route("api/test/[controller]")]
    [ApiController]
    public class UserLoginController : RickControllerBase
    {
        private readonly ILogger<OpenIdController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAppuserService _appuserService;

        public UserLoginController(ILogger<OpenIdController> logger, IAppuserService appuserService, IIdGeneratorService idGenerator)
        {
            _logger = logger;
            _appuserService = appuserService;
            _idGenerator = idGenerator;
        }

        /// <summary>
        /// 模拟登录
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<RickWebResult<UserTestLoginInfo>> Get([FromQuery] long userId, [FromQuery] long companyId = 15464799164941312)
        {
            Appuser appuser = await _appuserService.FindAsync<Appuser>(userId);
            UserInfo userInfo = new UserInfo();
            userInfo.Id = appuser.Id;
            userInfo.Mobile = appuser.Mobile;
            userInfo.Countrycode = appuser.Countrycode;
            userInfo.Openid = appuser.Openid;
            userInfo.Companyid = companyId;
            Syscompany syscompany = await _appuserService.FindAsync<Syscompany>(userInfo.Companyid);
            if (syscompany == null || syscompany.Status != 1)
            {
                return RickWebResult.Error(new UserTestLoginInfo(), 997, "授权已过期");
            }
            userInfo.Companyname = syscompany.Name;

            UserTestLoginInfo userLoginInfo = new UserTestLoginInfo() { Token = AuthTokenHelper.Create(userInfo) };
            return RickWebResult.Success(userLoginInfo);
        }
        public class UserTestLoginInfo
        {
            public string Token { get; set; }

        }

    }
}
