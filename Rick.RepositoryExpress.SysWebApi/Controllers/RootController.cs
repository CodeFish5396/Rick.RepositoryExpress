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
using Rick.RepositoryExpress.SysWebApi.Filters;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RootController : RickControllerBase
    {
        private readonly ILogger<RootController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly ISysuserService _sysuserService;
        private readonly RedisClientService _redisClientService;

        public RootController(ILogger<RootController> logger, ISysuserService sysuserService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _sysuserService = sysuserService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// Root用户登录，系统超级管理员权限
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Admin]
        public async Task<RickWebResult<UserLoginResult>> Get()
        {
            Sysuser sysuser = await _sysuserService.FindAsync<Sysuser>(t => t.Name == "root" && t.Password == Md5Helper.Create("fl2x_3QC") && t.Status == 1);
            UserInfo userInfo = new UserInfo();
            userInfo.Id = sysuser.Id;
            userInfo.Name = sysuser.Name;
            userInfo.IsDefaultRole = true;
            string token = AuthTokenHelper.Create(userInfo);
            UserLoginResult userLoginResult = new UserLoginResult();
            userLoginResult.Token = token;

            _redisClientService.HashSet(ConstString.RickUserLoginKey, userInfo.Id.ToString(), userLoginResult.Token);

            return RickWebResult.Success(userLoginResult);
        }

        [HttpPost]
        [Admin]
        public async Task<RickWebResult<object>> Post(RootInitRequest rootInitRequest)
        {
            await _sysuserService.BeginTransactionAsync();

            Sysuser sysuserAdmin = await _sysuserService.FindAsync<Sysuser>(t => t.Name == "root" && t.Password == Md5Helper.Create("fl2x_3QC") && t.Status == 1);

            DateTime now = DateTime.Now;
            Sysuser sysuser = new Sysuser();
            sysuser.Id = _idGenerator.NextId();
            sysuser.Name = rootInitRequest.UserName;
            sysuser.Password = Md5Helper.Create("a1Ab2Bc3C");
            sysuser.Truename = rootInitRequest.UserName;
            sysuser.Mobile = rootInitRequest.UserMobile;
            sysuser.Sex = (int)Gender.female;
            sysuser.Status = 1;
            sysuser.Addtime = now;
            sysuser.Lasttime = now;
            sysuser.Adduser = UserInfo.Id;
            sysuser.Lastuser = UserInfo.Id;

            await _sysuserService.AddAsync(sysuser);

            Syscompany syscompany = new Syscompany();
            syscompany.Id = _idGenerator.NextId();
            syscompany.Name = rootInitRequest.CompanyName;
            syscompany.Status = 1;
            syscompany.Addtime = now;
            syscompany.Lasttime = now;
            syscompany.Adduser = UserInfo.Id;
            syscompany.Lastuser = UserInfo.Id;
            await _sysuserService.AddAsync(syscompany);

            Sysusercompany sysusercompany = new Sysusercompany();
            sysusercompany.Id = _idGenerator.NextId();
            sysusercompany.Userid = sysuser.Id;
            sysusercompany.Companyid = syscompany.Id;
            sysusercompany.Status = 1;
            sysusercompany.Addtime = now;
            sysusercompany.Lasttime = now;
            sysusercompany.Adduser = UserInfo.Id;
            sysusercompany.Lastuser = UserInfo.Id;
            await _sysuserService.AddAsync(sysusercompany);

            Sysusercompany sysusercompanyAdmin = new Sysusercompany();
            sysusercompanyAdmin.Id = _idGenerator.NextId();
            sysusercompanyAdmin.Userid = UserInfo.Id;
            sysusercompanyAdmin.Companyid = syscompany.Id;
            sysusercompanyAdmin.Status = 1;
            sysusercompanyAdmin.Addtime = now;
            sysusercompanyAdmin.Lasttime = now;
            sysusercompanyAdmin.Adduser = UserInfo.Id;
            sysusercompanyAdmin.Lastuser = UserInfo.Id;
            await _sysuserService.AddAsync(sysusercompanyAdmin);

            Sysrole sysrole = new Sysrole();
            sysrole.Id = _idGenerator.NextId();
            sysrole.Companyid = syscompany.Id;
            sysrole.Name = ConstString.Admin;
            sysrole.Order = 0;
            sysrole.Status = 1;
            sysrole.Addtime = now;
            sysrole.Lasttime = now;
            sysrole.Adduser = UserInfo.Id;
            sysrole.Lastuser = UserInfo.Id;
            sysrole.Isdefault = 1;
            await _sysuserService.AddAsync(sysrole);

            Sysuserrole sysuserrole = new Sysuserrole();
            sysuserrole.Id = _idGenerator.NextId();
            sysuserrole.Companyid = syscompany.Id;
            sysuserrole.Userid = sysuser.Id;
            sysuserrole.Roleid = sysrole.Id;
            sysuserrole.Status = 1;
            sysuserrole.Addtime = now;
            sysuserrole.Lasttime = now;
            sysuserrole.Adduser = UserInfo.Id;
            sysuserrole.Lastuser = UserInfo.Id;
            await _sysuserService.AddAsync(sysuserrole);


            await _sysuserService.CommitAsync();

            return RickWebResult.Success(new object());
        }
        public class RootInitRequest
        {
            public string UserName { get; set; }
            public string CompanyName { get; set; }
            public string UserMobile { get; set; }

        }

    }
}
