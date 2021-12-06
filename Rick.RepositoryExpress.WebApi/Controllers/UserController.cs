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
            userLoginInfo.UserCode = appuser.Usercode;
            userLoginInfo.Openid = appuser.Openid;
            userLoginInfo.Mobile = appuser.Mobile;
            userLoginInfo.Countrycode = appuser.Countrycode;
            userLoginInfo.Truename = appuser.Truename;
            userLoginInfo.Countryname = appuser.Countryname;
            userLoginInfo.Cityname = appuser.Cityname;
            userLoginInfo.Gender = appuser.Gender;
            userLoginInfo.Birthdate = appuser.Birthdate;
            userLoginInfo.Email = appuser.Email;
            userLoginInfo.Address = appuser.Address;

            return RickWebResult.Success(userLoginInfo);
        }

        /// <summary>
        /// 修改用户名称
        /// </summary>
        /// <param name="userPostRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<UserLoginInfo>> Post([FromQuery] UserPostRequest userPostRequest)
        {
            await _appuserService.BeginTransactionAsync();
            Appuser appuser = await _appuserService.FindAsync<Appuser>(UserInfo.Id);
            appuser.Name = string.IsNullOrEmpty(userPostRequest.Name) ? appuser.Name : userPostRequest.Name;
            appuser.Truename = string.IsNullOrEmpty(userPostRequest.Truename) ? appuser.Truename : userPostRequest.Truename;
            appuser.Countryname = string.IsNullOrEmpty(userPostRequest.Countryname) ? appuser.Countryname : userPostRequest.Countryname;
            appuser.Cityname = string.IsNullOrEmpty(userPostRequest.Cityname) ? appuser.Cityname : userPostRequest.Cityname;
            appuser.Gender = string.IsNullOrEmpty(userPostRequest.Gender) ? appuser.Gender : userPostRequest.Gender;
            appuser.Birthdate = string.IsNullOrEmpty(userPostRequest.Birthdate) ? appuser.Birthdate : userPostRequest.Birthdate == "null" ? appuser.Birthdate : Convert.ToDateTime(userPostRequest.Birthdate);
            appuser.Email = string.IsNullOrEmpty(userPostRequest.Email) ? appuser.Email : userPostRequest.Email;
            appuser.Address = string.IsNullOrEmpty(userPostRequest.Address) ? appuser.Address : userPostRequest.Address;
            appuser.Lasttime = DateTime.Now;

            await _appuserService.UpdateAsync(appuser);
            await _appuserService.CommitAsync();

            UserLoginInfo userLoginInfo = new UserLoginInfo();
            userLoginInfo.Name = appuser.Name;
            userLoginInfo.UserCode = appuser.Usercode;
            userLoginInfo.Truename = appuser.Truename;
            userLoginInfo.Countryname = appuser.Countryname;
            userLoginInfo.Cityname = appuser.Cityname;
            userLoginInfo.Gender = appuser.Gender;
            userLoginInfo.Birthdate = appuser.Birthdate;
            userLoginInfo.Email = appuser.Email;
            userLoginInfo.Address = appuser.Address;

            userLoginInfo.Openid = appuser.Openid;
            userLoginInfo.Mobile = appuser.Mobile;
            userLoginInfo.Countrycode = appuser.Countrycode;
            return RickWebResult.Success(userLoginInfo);
        }

        public class UserLoginInfo
        {
            public string Name { get; set; }
            public string UserCode { get; set; }
            public string Openid { get; set; }
            public string Mobile { get; set; }
            public string Countrycode { get; set; }
            public string Truename { get; set; }
            public string Countryname { get; set; }
            public string Cityname { get; set; }
            public string Gender { get; set; }
            public DateTime? Birthdate { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }

        }
        public class UserPostRequest
        {
            public string Name { get; set; }
            public string Truename { get; set; }
            public string Countryname { get; set; }
            public string Cityname { get; set; }
            public string Gender { get; set; }
            public string Birthdate { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }

        }

    }
}
