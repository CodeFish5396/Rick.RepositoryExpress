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
    /// <summary>
    /// 用户充值清账
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserUnpayController : RickControllerBase
    {
        private readonly ILogger<UserUnpayController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageOrderApplyService _packageOrderApplyService;
        private readonly RedisClientService _redisClientService;

        public UserUnpayController(ILogger<UserUnpayController> logger, IPackageOrderApplyService packageOrderApplyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageOrderApplyService = packageOrderApplyService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询充值消费明细
        /// </summary>
        /// <param name="id"></param>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<UserUnpayConsumeResponseList>> Get([FromQuery] long id, [FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            var query = from consume in _packageOrderApplyService.Query<Appuseraccountconsume>(t => t.Chargeid == id
                        )
                        join packageorderapply in _packageOrderApplyService.Query<Packageorderapply>()
                        on consume.Orderid equals packageorderapply.Id
                        select new UserUnpayConsumeResponse()
                        {
                            Id = consume.Id,
                            Code = packageorderapply.Code,
                            Currencyid = (long)consume.Curencyid,
                            Paytime = Convert.ToDateTime(packageorderapply.Paytime),
                        };

            UserUnpayConsumeResponseList userChargeConsumeResponseList = new UserUnpayConsumeResponseList();
            userChargeConsumeResponseList.Count = await query.CountAsync();
            userChargeConsumeResponseList.List = await query.OrderByDescending(t => t.Paytime).Skip(pageSize * (index - 1)).Take(pageSize).ToListAsync();

            var currencyids = userChargeConsumeResponseList.List.Select(t => t.Currencyid).Distinct().ToList();
            var currencies = await _packageOrderApplyService.QueryAsync<Currency>(t => currencyids.Contains(t.Id));

            foreach (var item in userChargeConsumeResponseList.List)
            {
                item.Currencyname = currencies.Single(t => t.Id == item.Currencyid).Name;
            }
            return RickWebResult.Success(userChargeConsumeResponseList);
        }


        public class UserUnpayConsumeResponse
        {
            public long Id { get; set; }
            public string Code { get; set; }
            public long Currencyid { get; set; }
            public string Currencyname { get; set; }
            public DateTime Paytime { get; set; }
        }

        public class UserUnpayConsumeResponseList
        {
            public int Count { get; set; }
            public List<UserUnpayConsumeResponse> List { get; set; }

        }

    }

}
