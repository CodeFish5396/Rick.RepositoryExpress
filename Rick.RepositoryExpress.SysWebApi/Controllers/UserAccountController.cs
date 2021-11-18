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
using Microsoft.EntityFrameworkCore;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
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
        /// 查询用户账户
        /// </summary>
        /// <param name="status"></param>
        /// <param name="currencyid"></param>
        /// <param name="userid"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<UserAccountResponseList>> Get([FromQuery] int? status, [FromQuery] long? currencyid, [FromQuery] long? userid, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from account in _appuseraccountService.Query<Appuseraccount>(t => (!status.HasValue || t.Status == status) && (!currencyid.HasValue || t.Currencyid == currencyid) && (!userid.HasValue || t.Appuser == userid))
                        join currency in _appuseraccountService.Query<Currency>(t => true)
                        on account.Currencyid equals currency.Id
                        join user in _appuseraccountService.Query<Appuser>(t => true)
                        on account.Appuser equals user.Id
                        select new UserAccountResponse()
                        {
                            Userid = account.Appuser,
                            Username = user.Name,
                            Currencyid = account.Currencyid,
                            CurrencyName = currency.Name,
                            Amount = account.Amount
                        };

            UserAccountResponseList userAccountResponseList = new UserAccountResponseList();
            userAccountResponseList.Count = await query.CountAsync();
            userAccountResponseList.List = await query.OrderBy(t => t.Userid).Skip(pageSize * (index - 1)).Take(pageSize).ToListAsync();
            return RickWebResult.Success(userAccountResponseList);
        }

        public class UserAccountResponse
        {
            public long Userid { get; set; }
            public string Username { get; set; }
            public long Currencyid { get; set; }
            public string CurrencyName { get; set; }
            public decimal Amount { get; set; }
        }

        public class UserAccountResponseList
        {
            public int Count { get; set; }
            public IList<UserAccountResponse> List { get; set; }

        }

    }
}
