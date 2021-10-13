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
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : RickControllerBase
    {
        private readonly ILogger<OpenIdController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAppuserService _appuserService;

        public UserController(ILogger<OpenIdController> logger, IAppuserService appuserService, IIdGeneratorService idGenerator)
        {
            _logger = logger;
            _appuserService = appuserService;
            _idGenerator = idGenerator;
        }
        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<UserLoginInfo>> Get()
        {
            Appuser appuser = await _appuserService.FindAsync<Appuser>(UserInfo.Id);

            UserLoginInfo userLoginInfo = new UserLoginInfo();
            userLoginInfo.Name = appuser.Name;
            userLoginInfo.Openid = appuser.Openid;
            userLoginInfo.Mobile = appuser.Mobile;
            userLoginInfo.Countrycode = appuser.Countrycode;
            return RickWebResult.Success(userLoginInfo);
        }

        public class UserLoginInfo
        {
            public string Name { get; set; }
            public string Openid { get; set; }
            public string Mobile { get; set; }
            public string Countrycode { get; set; }

        }

    }
}
