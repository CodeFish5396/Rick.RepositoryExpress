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
    [Route("api/[controller]")]
    [ApiController]
    public class UseraccountchargeController : RickControllerBase
    {
        private readonly ILogger<UseraccountchargeController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IAppuseraccountService _appuseraccountService;
        private readonly RedisClientService _redisClientService;

        public UseraccountchargeController(ILogger<UseraccountchargeController> logger, IAppuseraccountService appuseraccountService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _appuseraccountService = appuseraccountService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询用户充值
        /// </summary>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<AppuseraccountchargeResponseList>> Get([FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            AppuseraccountchargeResponseList appuseraccountchargeResponseList = new AppuseraccountchargeResponseList();

            var query = from charge in _appuseraccountService.Query<Appuseraccountcharge>(t => t.Status == 1 && t.Appuser == UserInfo.Id)
                        join currency in _appuseraccountService.Query<Currency>()
                        on charge.Currencyid equals currency.Id
                        select new AppuseraccountchargeResponse() {
                            Id = charge.Id,
                            Amount = charge.Amount,
                            Addtime = charge.Addtime,
                            Currencyid = charge.Currencyid,
                            CurrencyName = currency.Name
                        };
            appuseraccountchargeResponseList.Count = await query.CountAsync();
            appuseraccountchargeResponseList.List = await query.OrderByDescending(t => t.Addtime).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            return RickWebResult.Success(appuseraccountchargeResponseList);
        }
        public class AppuseraccountchargeResponse
        {
            public long Id { get; set; }
            public decimal Amount { get; set; }
            public DateTime Addtime { get; set; }
            public long Currencyid { get; set; }
            public string CurrencyName { get; set; }
        }
        public class AppuseraccountchargeResponseList
        { 
            public int Count { get; set; }
            public List<AppuseraccountchargeResponse> List { get; set; }
        }

    }
}
