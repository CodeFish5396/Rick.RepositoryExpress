using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rick.RepositoryExpress.WebApi.Models;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using Rick.RepositoryExpress.Common;
using Microsoft.AspNetCore.Authorization;
using Rick.RepositoryExpress.Utils.Wechat;
using Rick.RepositoryExpress.RedisService;

namespace Rick.RepositoryExpress.WebApi.Controllers
{
    [Route("api/user/[controller]")]
    [ApiController]
    public class OpenIdController : RickControllerBase
    {
        private readonly ILogger<OpenIdController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAppuserService _appuserService;
        private readonly RedisClientService _redisClientService;

        public OpenIdController(ILogger<OpenIdController> logger, IAppuserService appuserService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _appuserService = appuserService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 获取openid
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<RickWebResult<MiniProgramLoginResult>> Get([FromQuery] string code)
        {
            MiniProgramLoginResult miniProgramLoginResult = await MiniProgramHelper.GetOpenidAndSessionkey(code);
            _redisClientService.StringSet(miniProgramLoginResult.OpenId, miniProgramLoginResult.Session_Key);
            return RickWebResult.Success(miniProgramLoginResult);
        }

        /// <summary>
        /// 解密获得手机号、注册并登录
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<RickWebResult<OpenIdResponseData>> Post([FromBody] OpenIdRequestData data)
        {
            string session_Key = _redisClientService.StringGet(data.OpenId);
            string messge;
            MiniProgramUserResult miniProgramUserResult = MiniProgramHelper.Descrypt(data.MobileNo, session_Key, data.Iv, out messge);
            if (string.IsNullOrEmpty(messge))
            {
                await _appuserService.BeginTransactionAsync();

                Appuser appuser = await _appuserService.FindAsync<Appuser>(t => t.Openid == data.OpenId);
                if (appuser == null || appuser.Id <= 0)
                {
                    appuser = new Appuser();
                    appuser.Id = _idGenerator.NextId();
                    appuser.Openid = data.OpenId;
                    appuser.Mobile = miniProgramUserResult.PurePhoneNumber;
                    appuser.Countrycode = miniProgramUserResult.Countrycode;
                    DateTime now = DateTime.Now;
                    appuser.Addtime = now;
                    appuser.Lasttime = now;
                    appuser.Status = 1;
                    appuser.AddUser = appuser.Id;
                    await _appuserService.AddAsync(appuser);
                    await _appuserService.CommitAsync();
                }
                else
                {
                    appuser.Mobile = miniProgramUserResult.PurePhoneNumber;
                    appuser.Countrycode = miniProgramUserResult.Countrycode;
                    DateTime now = DateTime.Now;
                    appuser.Lasttime = now;
                    await _appuserService.UpdateAsync(appuser);
                    await _appuserService.CommitAsync();
                }
                OpenIdResponseData openIdResponseData = new OpenIdResponseData();
                openIdResponseData.OpenId = appuser.Openid;
                openIdResponseData.Mobile = appuser.Mobile;
                openIdResponseData.Countrycode = appuser.Countrycode;
                UserInfo userInfo = new UserInfo();
                userInfo.Id = appuser.Id;
                userInfo.Mobile = appuser.Mobile;
                userInfo.Countrycode = appuser.Countrycode;
                userInfo.Openid = appuser.Openid;
                userInfo.Companyid = data.CompanyId <= 0 ? 15464799164941312 : data.CompanyId;
                Syscompany syscompany = await _appuserService.FindAsync<Syscompany>(userInfo.Companyid);
                if (syscompany == null || syscompany.Status != 1)
                {
                    return RickWebResult.Error(new OpenIdResponseData(), 997, "授权已过期");
                }
                userInfo.Companyname = syscompany.Name;
                openIdResponseData.Token = AuthTokenHelper.Create(userInfo);

                return RickWebResult.Success(openIdResponseData);
            }
            else
            {
                _logger.LogError(messge);
                return RickWebResult.Error(new OpenIdResponseData(), 901, messge);
            }
        }

        public class OpenIdRequestData
        {
            public string MobileNo { get; set; }
            public string OpenId { get; set; }
            public string Iv { get; set; }
            public long CompanyId { get; set; }

        }

        public class OpenIdResponseData
        {
            public string OpenId { get; set; }

            public string Mobile { get; set; }
            public string Countrycode { get; set; }
            /// <summary>
            /// 用户登录凭证，消息头里Token携带
            /// </summary>
            public string Token { get; set; }

        }

    }
}
