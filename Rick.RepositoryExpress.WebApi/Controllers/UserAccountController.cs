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
using Rick.RepositoryExpress.Utils;
using Microsoft.EntityFrameworkCore;

namespace Rick.RepositoryExpress.WebApi.Controllers
{
    /// <summary>
    /// 用户账户
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserAccountController : RickControllerBase
    {
        private readonly ILogger<UserAccountController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAppuseraccountService _appuseraccountService;
        private readonly RedisClientService _redisClientService;

        public UserAccountController(ILogger<UserAccountController> logger, IAppuseraccountService appuseraccountService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _appuseraccountService = appuseraccountService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 获取账户余额
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<List<UserAccountResponse>>> Get()
        {
            var result = await (from useraccount
                         in _appuseraccountService.Query<Appuseraccount>(t => t.Status == 1 && t.Appuser == UserInfo.Id)
                         join currency
                         in _appuseraccountService.Query<Currency>(t => 1 == 1)
                         on useraccount.Currencyid equals currency.Id
                         select new UserAccountResponse()
                         {
                             Currencyid = useraccount.Currencyid,
                             CurrencyName = currency.Name,
                             Amount = useraccount.Amount
                         }).ToListAsync();
            return RickWebResult.Success(result);
        }

        /// <summary>
        /// 用户充值
        /// </summary>
        /// <param name="userAccountResquest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] UserAccountResquest userAccountResquest)
        {
            await _appuseraccountService.BeginTransactionAsync();
            DateTime now = DateTime.Now;

            Appuseraccountcharge appuseraccountcharge = new Appuseraccountcharge();
            appuseraccountcharge.Id = _idGenerator.NextId();
            appuseraccountcharge.Status = 2;
            appuseraccountcharge.Adduser = UserInfo.Id;
            appuseraccountcharge.Addtime = now;
            appuseraccountcharge.Appuser = UserInfo.Id;

            await _appuseraccountService.AddAsync(appuseraccountcharge);

            foreach (long fileInfoId in userAccountResquest.Images)
            {
                Appuseraccountchargeimage appuseraccountchargeimage = new Appuseraccountchargeimage();
                appuseraccountchargeimage.Id = _idGenerator.NextId();
                appuseraccountchargeimage.Fileinfoid = fileInfoId;
                appuseraccountchargeimage.Appuseraccountchargeid = appuseraccountcharge.Id;
                appuseraccountchargeimage.Status = 1;
                appuseraccountchargeimage.Adduser = UserInfo.Id;
                appuseraccountchargeimage.Addtime = now;
                await _appuseraccountService.AddAsync(appuseraccountchargeimage);
            }
            Appuser appuser = await _appuseraccountService.FindAsync<Appuser>(UserInfo.Id);

            Message message = new Message();
            message.Id = _idGenerator.NextId();
            message.Status = 1;
            message.Adduser = appuser.Id;
            message.Lastuser = appuser.Id;
            message.Addtime = now;
            message.Lasttime = now;
            message.Isclosed = 0;
            message.Sender = UserInfo.Id;
            message.Index = "userAccountCharge";
            message.Message1 = string.Format("用户:{0}提交充值，请审核", appuser.Usercode);
            await _appuseraccountService.AddAsync(message);

            await _appuseraccountService.CommitAsync();
            return RickWebResult.Success(new object());
        }

        public class UserAccountResponse
        {
            public long Currencyid { get; set; }
            public string CurrencyName { get; set; }
            public decimal Amount { get; set; }
        }

        public class UserAccountResquest
        { 
            public IList<long> Images { get; set; }
        }

    }
}
